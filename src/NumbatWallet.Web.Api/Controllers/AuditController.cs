using NumbatWallet.Infrastructure.EventSourcing;
using NumbatWallet.Web.Api.Security;
using System.Security.Claims;

namespace NumbatWallet.Web.Api.Controllers;

/// <summary>
/// Controller for audit trail and event sourcing queries
/// POA: Provides complete audit trail functionality
/// </summary>
[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Auditor")]
[Produces("application/json")]
public class AuditController : ControllerBase
{
    private readonly IEventSourcingService _eventSourcingService;
    private readonly IEventStore _eventStore;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(
        IEventSourcingService eventSourcingService,
        IEventStore eventStore,
        ISecurityAuditService auditService,
        ILogger<AuditController> logger)
    {
        _eventSourcingService = eventSourcingService;
        _eventStore = eventStore;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Get audit trail for a specific aggregate
    /// </summary>
    [HttpGet("trail/{aggregateId:guid}")]
    [ProducesResponseType(typeof(EventSourcingAuditDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditTrail(Guid aggregateId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Retrieving audit trail for aggregate {AggregateId} by user {UserId}",
            aggregateId, userId);

        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.DataAccess,
            $"Audit trail access: {aggregateId}");

        var audit = await _eventSourcingService.GetAuditTrailAsync(aggregateId);

        if (audit == null || !audit.Events.Any())
        {
            return NotFound($"No audit trail found for aggregate {aggregateId}");
        }

        return Ok(audit);
    }

    /// <summary>
    /// Get audit trails by aggregate type within a date range
    /// </summary>
    [HttpGet("trail/type/{aggregateType}")]
    [ProducesResponseType(typeof(IEnumerable<EventSourcingAuditDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditTrailsByType(
        string aggregateType,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        if (to < from)
        {
            return BadRequest("End date must be after start date");
        }

        if ((to - from).TotalDays > 90)
        {
            return BadRequest("Date range cannot exceed 90 days");
        }

        _logger.LogInformation("Retrieving audit trails for type {AggregateType} from {From} to {To}",
            aggregateType, from, to);

        var audits = await _eventSourcingService.GetAuditTrailByTypeAsync(aggregateType, from, to);

        return Ok(audits);
    }

    /// <summary>
    /// Get aggregate state at a specific version
    /// </summary>
    [HttpGet("state/{aggregateId:guid}/version/{aggregateVersion:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAggregateAtVersion(Guid aggregateId, int aggregateVersion)
    {
        _logger.LogInformation("Retrieving aggregate {AggregateId} at version {Version}",
            aggregateId, aggregateVersion);

        // For security, we'll return a generic object representation
        // In production, you might want to be more specific about the type
        var aggregate = await _eventSourcingService.GetAggregateAtVersionAsync<object>(aggregateId, aggregateVersion);

        if (aggregate == null)
        {
            return NotFound($"Aggregate {aggregateId} not found at version {aggregateVersion}");
        }

        return Ok(aggregate);
    }

    /// <summary>
    /// Get aggregate state as of a specific date
    /// </summary>
    [HttpGet("state/{aggregateId:guid}/date")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAggregateAtDate(
        Guid aggregateId,
        [FromQuery] DateTime asOf)
    {
        if (asOf > DateTime.UtcNow)
        {
            return BadRequest("Cannot query future dates");
        }

        _logger.LogInformation("Retrieving aggregate {AggregateId} as of {AsOf}",
            aggregateId, asOf);

        var aggregate = await _eventSourcingService.GetAggregateAtDateAsync<object>(aggregateId, asOf);

        if (aggregate == null)
        {
            return NotFound($"Aggregate {aggregateId} not found as of {asOf:yyyy-MM-dd}");
        }

        return Ok(aggregate);
    }

    /// <summary>
    /// Create a snapshot for an aggregate
    /// </summary>
    [HttpPost("snapshot/{aggregateId:guid}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSnapshot(Guid aggregateId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Creating snapshot for aggregate {AggregateId} by user {UserId}",
            aggregateId, userId);

        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.ConfigurationChange,
            $"Snapshot created for aggregate: {aggregateId}");

        await _eventSourcingService.CreateSnapshotAsync(aggregateId);

        return CreatedAtAction(
            nameof(GetLatestSnapshot),
            new { aggregateId },
            new { message = $"Snapshot created for aggregate {aggregateId}" });
    }

    /// <summary>
    /// Get the latest snapshot for an aggregate
    /// </summary>
    [HttpGet("snapshot/{aggregateId:guid}")]
    [ProducesResponseType(typeof(EventSnapshot), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestSnapshot(Guid aggregateId)
    {
        _logger.LogInformation("Retrieving latest snapshot for aggregate {AggregateId}", aggregateId);

        var snapshot = await _eventStore.GetLatestSnapshotAsync(aggregateId);

        if (snapshot == null)
        {
            return NotFound($"No snapshot found for aggregate {aggregateId}");
        }

        return Ok(snapshot);
    }

    /// <summary>
    /// Get all events with pagination
    /// </summary>
    [HttpGet("events")]
    [ProducesResponseType(typeof(IEnumerable<StoredEvent>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1)
        {
            return BadRequest("Page number must be greater than 0");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest("Page size must be between 1 and 100");
        }

        _logger.LogInformation("Retrieving events page {Page} with size {PageSize}", page, pageSize);

        var events = await _eventStore.GetAllEventsAsync(page, pageSize);

        return Ok(events);
    }

    /// <summary>
    /// Get events for a specific aggregate
    /// </summary>
    [HttpGet("events/{aggregateId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<StoredEvent>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAggregateEvents(Guid aggregateId)
    {
        _logger.LogInformation("Retrieving events for aggregate {AggregateId}", aggregateId);

        var events = await _eventStore.GetEventsAsync(aggregateId);

        if (!events.Any())
        {
            return NotFound($"No events found for aggregate {aggregateId}");
        }

        return Ok(events);
    }

    /// <summary>
    /// Get audit statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(AuditStatisticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditStatistics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var endDate = to ?? DateTime.UtcNow;

        if (endDate < startDate)
        {
            return BadRequest("End date must be after start date");
        }

        _logger.LogInformation("Retrieving audit statistics from {From} to {To}", startDate, endDate);

        // Get events within the date range for statistics
        var walletEvents = await _eventSourcingService.GetAuditTrailByTypeAsync("Wallet", startDate, endDate);
        var credentialEvents = await _eventSourcingService.GetAuditTrailByTypeAsync("Credential", startDate, endDate);

        var statistics = new AuditStatisticsDto
        {
            Period = new AuditDateRangeDto { From = startDate, To = endDate },
            TotalEvents = walletEvents.Sum(a => a.Events.Count) + credentialEvents.Sum(a => a.Events.Count),
            EventsByType = new Dictionary<string, int>
            {
                ["Wallet"] = walletEvents.Sum(a => a.Events.Count),
                ["Credential"] = credentialEvents.Sum(a => a.Events.Count)
            },
            AggregatesModified = walletEvents.Count() + credentialEvents.Count(),
            TopUsers = new List<UserActivityDto>() // Would need to aggregate from events
        };

        return Ok(statistics);
    }
}

// DTOs for audit statistics
public class AuditStatisticsDto
{
    public AuditDateRangeDto Period { get; set; } = new();
    public int TotalEvents { get; set; }
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public int AggregatesModified { get; set; }
    public List<UserActivityDto> TopUsers { get; set; } = new();
}

public class AuditDateRangeDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public class UserActivityDto
{
    public string UserId { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public DateTime LastActivity { get; set; }
}