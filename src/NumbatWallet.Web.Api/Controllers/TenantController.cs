using NumbatWallet.Application.Commands.Tenants;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Queries.Tenants;

namespace NumbatWallet.Web.Api.Controllers;

/// <summary>
/// Tenant management API
/// POA: Real implementation for multi-tenant operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,TenantAdmin")]
public class TenantController : ControllerBase
{
    private readonly ICommandHandler<CreateTenantCommand, Guid> _createHandler;
    private readonly ICommandHandler<UpdateTenantCommand> _updateHandler;
    private readonly ICommandHandler<DeleteTenantCommand> _deleteHandler;
    private readonly IQueryHandler<GetTenantByIdQuery, TenantDto?> _getByIdHandler;
    private readonly IQueryHandler<GetAllTenantsQuery, IEnumerable<TenantDto>> _getAllHandler;
    private readonly ILogger<TenantController> _logger;

    public TenantController(
        ICommandHandler<CreateTenantCommand, Guid> createHandler,
        ICommandHandler<UpdateTenantCommand> updateHandler,
        ICommandHandler<DeleteTenantCommand> deleteHandler,
        IQueryHandler<GetTenantByIdQuery, TenantDto?> getByIdHandler,
        IQueryHandler<GetAllTenantsQuery, IEnumerable<TenantDto>> getAllHandler,
        ILogger<TenantController> logger)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _getByIdHandler = getByIdHandler;
        _getAllHandler = getAllHandler;
        _logger = logger;
    }

    /// <summary>
    /// Get all tenants
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TenantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly = true)
    {
        _logger.LogInformation("Getting all tenants. ActiveOnly: {ActiveOnly}", activeOnly);

        var query = new GetAllTenantsQuery { ActiveOnly = activeOnly ?? true };
        var result = await _getAllHandler.HandleAsync(query);

        return Ok(result);
    }

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("Getting tenant {TenantId}", id);

        var query = new GetTenantByIdQuery { Id = id };
        var result = await _getByIdHandler.HandleAsync(query);

        if (result == null)
        {
            return NotFound($"Tenant {id} not found");
        }

        return Ok(result);
    }

    /// <summary>
    /// Create new tenant
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateTenantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request)
    {
        _logger.LogInformation("Creating new tenant: {TenantName}", request.Name);

        var command = new CreateTenantCommand(
            request.Name,
            request.Identifier,
            request.SubscriptionTier ?? "Basic",
            request.Settings ?? new Dictionary<string, object>()
        );

        var tenantId = await _createHandler.HandleAsync(command);

        var response = new CreateTenantResponse
        {
            Id = tenantId,
            Message = $"Tenant '{request.Name}' created successfully"
        };

        return CreatedAtAction(nameof(GetById), new { id = tenantId }, response);
    }

    /// <summary>
    /// Update tenant
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantRequest request)
    {
        _logger.LogInformation("Updating tenant {TenantId}", id);

        var command = new UpdateTenantCommand(
            id,
            request.Name,
            request.IsActive,
            request.SubscriptionTier,
            request.Settings
        );

        await _updateHandler.HandleAsync(command);

        return NoContent();
    }

    /// <summary>
    /// Delete tenant (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("Deleting tenant {TenantId}", id);

        var command = new DeleteTenantCommand { Id = id };
        await _deleteHandler.HandleAsync(command);

        return NoContent();
    }

    /// <summary>
    /// Get tenant statistics
    /// </summary>
    [HttpGet("{id:guid}/statistics")]
    [ProducesResponseType(typeof(TenantStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatistics(Guid id)
    {
        _logger.LogInformation("Getting statistics for tenant {TenantId}", id);

        // TODO: Implement statistics query handler
        var stats = new TenantStatistics
        {
            TenantId = id,
            UserCount = 0,
            WalletCount = 0,
            CredentialCount = 0,
            ActiveSessions = 0,
            StorageUsedMB = 0,
            LastActivityDate = DateTime.UtcNow
        };

        return Ok(stats);
    }
}

// Request/Response DTOs
public record CreateTenantRequest
{
    public required string Name { get; init; }
    public required string Identifier { get; init; }
    public string? SubscriptionTier { get; init; }
    public Dictionary<string, object>? Settings { get; init; }
}

public record UpdateTenantRequest
{
    public string? Name { get; init; }
    public bool? IsActive { get; init; }
    public string? SubscriptionTier { get; init; }
    public Dictionary<string, object>? Settings { get; init; }
}

public record CreateTenantResponse
{
    public Guid Id { get; init; }
    public string Message { get; init; } = string.Empty;
}

public record TenantStatistics
{
    public Guid TenantId { get; init; }
    public int UserCount { get; init; }
    public int WalletCount { get; init; }
    public int CredentialCount { get; init; }
    public int ActiveSessions { get; init; }
    public decimal StorageUsedMB { get; init; }
    public DateTime LastActivityDate { get; init; }
}
