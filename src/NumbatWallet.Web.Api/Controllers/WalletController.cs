using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Wallets.Commands.CreateWallet;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Web.Api.Controllers;

/// <summary>
/// Wallet management API
/// POA: Real implementation for wallet operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize]
public class WalletController : ControllerBase
{
    private readonly ICommandHandler<CreateWalletCommand, WalletDto> _createHandler;
    private readonly ILogger<WalletController> _logger;

    public WalletController(
        ICommandHandler<CreateWalletCommand, WalletDto> createHandler,
        ILogger<WalletController> logger)
    {
        _createHandler = createHandler;
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
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning("Validation failed for wallet creation: {Errors}",
                string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)));
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
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
    /// Get wallet by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWallet(Guid id, CancellationToken cancellationToken)
    {
        // TODO: Implement GetWalletByIdQuery handler
        _logger.LogInformation("Getting wallet {WalletId}", id);
        return NotFound(new { error = $"Wallet {id} not found" });
    }

    /// <summary>
    /// Get wallets for a person
    /// </summary>
    [HttpGet("person/{personId:guid}")]
    [ProducesResponseType(typeof(List<WalletDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPersonWallets(
        Guid personId,
        CancellationToken cancellationToken)
    {
        // TODO: Implement GetWalletsByPersonQuery
        _logger.LogInformation("Getting wallets for person {PersonId}", personId);
        return Ok(new List<WalletDto>());
    }

    /// <summary>
    /// Activate a wallet
    /// </summary>
    [HttpPut("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateWallet(
        Guid id,
        CancellationToken cancellationToken)
    {
        // TODO: Implement ActivateWalletCommand
        _logger.LogInformation("Activating wallet {WalletId}", id);
        return NoContent();
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
        // TODO: Implement DeleteWalletCommand
        _logger.LogInformation("Deleting wallet {WalletId}", id);
        return NoContent();
    }

    private Guid GetTenantId()
    {
        // TODO: Get from claims/context
        var tenantClaim = User.FindFirst("TenantId")?.Value;
        if (Guid.TryParse(tenantClaim, out var tenantId))
        {
            return tenantId;
        }

        // Default for development
        return Guid.Parse("a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11");
    }
}

/// <summary>
/// Request to create a wallet
/// </summary>
public class CreateWalletRequest
{
    public Guid PersonId { get; set; }
    public string Type { get; set; } = "HOLDER";
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