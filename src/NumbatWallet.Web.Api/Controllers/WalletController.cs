using NumbatWallet.Application.Commands.Wallets;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Queries.Wallets;
using NumbatWallet.Application.Wallets.Commands.CreateWallet;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Web.Api.Controllers;

/// <summary>
/// Wallet management API
/// POA: Real implementation for wallet operations
/// </summary>
[ApiController]
[Route("api/v1/wallet")]
[Route("api/v1/wallets")] // Support plural for backwards compatibility
[Microsoft.AspNetCore.Authorization.Authorize]
public class WalletController : ControllerBase
{
    private readonly ICommandHandler<CreateWalletCommand, WalletDto> _createHandler;
    private readonly IQueryHandler<GetWalletByIdQuery, WalletDto?> _getWalletHandler;
    private readonly IQueryHandler<GetWalletsByPersonQuery, IEnumerable<WalletDto>> _getWalletsByPersonHandler;
    private readonly ICommandHandler<ActivateWalletCommand, WalletDto> _activateHandler;
    private readonly ICommandHandler<DeleteWalletCommand, bool> _deleteHandler;
    private readonly ILogger<WalletController> _logger;

    public WalletController(
        ICommandHandler<CreateWalletCommand, WalletDto> createHandler,
        IQueryHandler<GetWalletByIdQuery, WalletDto?> getWalletHandler,
        IQueryHandler<GetWalletsByPersonQuery, IEnumerable<WalletDto>> getWalletsByPersonHandler,
        ICommandHandler<ActivateWalletCommand, WalletDto> activateHandler,
        ICommandHandler<DeleteWalletCommand, bool> deleteHandler,
        ILogger<WalletController> logger)
    {
        _createHandler = createHandler;
        _getWalletHandler = getWalletHandler;
        _getWalletsByPersonHandler = getWalletsByPersonHandler;
        _activateHandler = activateHandler;
        _deleteHandler = deleteHandler;
        _logger = logger;
    }

    /// <summary>
    /// Create a new wallet
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateWalletResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateWallet(
        [FromBody] CreateWalletRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateWalletCommand
            {
                PersonId = request.PersonId,
                Name = request.Name,
                Type = Enum.Parse<WalletType>(request.Type, true),
                TenantId = GetTenantId().ToString()
            };

            var walletDto = await _createHandler.HandleAsync(command, cancellationToken);

            _logger.LogInformation("Wallet {WalletId} created for person {PersonId}",
                walletDto.Id, request.PersonId);

            return CreatedAtAction(
                nameof(GetWallet),
                new { id = walletDto.Id },
                new CreateWalletResponse { WalletId = Guid.Parse(walletDto.Id) });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation failed for wallet creation: {Errors}",
                string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)));
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid argument for wallet creation: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Application.Common.Exceptions.ConflictException ex)
        {
            // Business conflict (e.g. person already has a wallet) is a 409, not a 500.
            _logger.LogWarning("Wallet creation conflict: {Error}", ex.Message);
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Wallet creation failed: {Error}", ex.Message);
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating wallet");
            return StatusCode(500, new { error = "An error occurred while creating the wallet" });
        }
    }

    /// <summary>
    /// Get all wallets for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<WalletDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyWallets(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var personId))
        {
            return Ok(new List<WalletDto>()); // Return empty list if no valid user ID
        }

        _logger.LogInformation("Getting wallets for person {PersonId}", personId);

        var query = new GetWalletsByPersonQuery(personId);
        var wallets = await _getWalletsByPersonHandler.HandleAsync(query, cancellationToken);

        return Ok(wallets);
    }

    /// <summary>
    /// Get wallet by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWallet(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting wallet {WalletId}", id);

        var query = new GetWalletByIdQuery(id);
        var wallet = await _getWalletHandler.HandleAsync(query, cancellationToken);

        if (wallet == null)
        {
            return NotFound(new { error = $"Wallet {id} not found" });
        }

        // SECURITY: prevent IDOR — a non-privileged caller may only read their own wallet.
        // Return 404 (not 403) so wallet existence isn't disclosed to non-owners.
        if (!IsAdminOrOfficer() && !OwnsPerson(wallet.PersonId))
        {
            _logger.LogWarning("Blocked cross-user wallet access: caller {Caller} requested wallet {WalletId} owned by {Owner}",
                GetCallerPersonId(), id, wallet.PersonId);
            return NotFound(new { error = $"Wallet {id} not found" });
        }

        return Ok(wallet);
    }

    /// <summary>
    /// Get wallets for a person
    /// </summary>
    [HttpGet("person/{personId:guid}")]
    [ProducesResponseType(typeof(List<WalletDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPersonWallets(
        Guid personId,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        // SECURITY: prevent IDOR — only Admin/Officer may list another person's wallets.
        if (!IsAdminOrOfficer() && !OwnsPerson(personId.ToString()))
        {
            _logger.LogWarning("Blocked cross-user wallet listing: caller {Caller} requested wallets for person {PersonId}",
                GetCallerPersonId(), personId);
            return Forbid();
        }

        _logger.LogInformation("Getting wallets for person {PersonId}", personId);

        var query = new GetWalletsByPersonQuery(personId, includeInactive);
        var wallets = await _getWalletsByPersonHandler.HandleAsync(query, cancellationToken);

        return Ok(wallets);
    }

    /// <summary>
    /// Activate a wallet
    /// </summary>
    [HttpPut("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateWallet(
        Guid id,
        [FromBody] ActivateWalletRequest? request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activating wallet {WalletId}", id);

        try
        {
            var command = new ActivateWalletCommand(id, request?.Pin);
            var wallet = await _activateHandler.HandleAsync(command, cancellationToken);

            return Ok(wallet);
        }
        catch (Application.Common.Exceptions.EntityNotFoundException)
        {
            return NotFound(new { error = $"Wallet {id} not found" });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete (deactivate) a wallet
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,TenantAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWallet(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting wallet {WalletId}", id);

        try
        {
            var command = new DeleteWalletCommand(id);
            await _deleteHandler.HandleAsync(command, cancellationToken);

            return NoContent();
        }
        catch (Application.Common.Exceptions.EntityNotFoundException)
        {
            return NotFound(new { error = $"Wallet {id} not found" });
        }
        catch (SharedKernel.Exceptions.BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>The authenticated caller's person id (from the NameIdentifier claim), if any.</summary>
    private Guid? GetCallerPersonId()
    {
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var g) ? g : null;
    }

    /// <summary>True when the caller holds a privileged role allowed to access any person's resources.</summary>
    private bool IsAdminOrOfficer()
        => User.IsInRole("Admin") || User.IsInRole("Officer") || User.IsInRole("TenantAdmin");

    /// <summary>True when the supplied person id matches the authenticated caller's person id.</summary>
    private bool OwnsPerson(string? personId)
    {
        var caller = GetCallerPersonId();
        return caller is not null
            && !string.IsNullOrEmpty(personId)
            && string.Equals(personId, caller.Value.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private Guid GetTenantId()
    {
        var tenantClaim = User.FindFirst("tenant_id")?.Value
            ?? User.FindFirst("TenantId")?.Value
            ?? User.FindFirst("tid")?.Value;
        if (Guid.TryParse(tenantClaim, out var tenantId))
        {
            return tenantId;
        }

        // SECURITY: fail closed. Never fall back to a hardcoded tenant — that would let an
        // authenticated request with a missing/invalid tenant claim write into another tenant.
        throw new InvalidOperationException("A valid tenant context (tenant_id claim) is required.");
    }
}

/// <summary>
/// Request to create a wallet
/// </summary>
public class CreateWalletRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public Guid PersonId { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 1)]
    public string Type { get; set; } = "HOLDER";

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Response from wallet creation
/// </summary>
public class CreateWalletResponse
{
    public Guid WalletId { get; set; }
}

/// <summary>
/// Request to activate a wallet
/// </summary>
public class ActivateWalletRequest
{
    public string? Pin { get; set; }
}
