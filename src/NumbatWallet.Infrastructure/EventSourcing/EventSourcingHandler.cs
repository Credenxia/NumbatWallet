using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.SharedKernel.Primitives;

namespace NumbatWallet.Infrastructure.EventSourcing;

/// <summary>
/// Handles automatic persistence of domain events
/// </summary>
public interface IEventSourcingHandler
{
    Task HandleEventsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Event sourcing handler that processes domain events from entities
/// </summary>
public class EventSourcingHandler : IEventSourcingHandler
{
    private readonly NumbatWalletDbContext _context;
    private readonly IEventStore _eventStore;
    private readonly ILogger<EventSourcingHandler> _logger;

    public EventSourcingHandler(
        NumbatWalletDbContext context,
        IEventStore eventStore,
        ILogger<EventSourcingHandler> logger)
    {
        _context = context;
        _eventStore = eventStore;
        _logger = logger;
    }

    public async Task HandleEventsAsync(CancellationToken cancellationToken = default)
    {
        var entities = _context.ChangeTracker
            .Entries<Entity<Guid>>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        if (!entities.Any())
        {
            return;
        }

        var events = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        _logger.LogDebug("Processing {Count} domain events from {EntityCount} entities",
            events.Count, entities.Count);

        // Save events to event store
        await _eventStore.SaveEventsAsync(events, cancellationToken);

        // Clear domain events from entities
        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }

        _logger.LogInformation("Persisted {Count} domain events to event store", events.Count);
    }
}

/// <summary>
/// Interceptor to automatically save domain events
/// </summary>
public class EventSourcingInterceptor : SaveChangesInterceptor
{
    private readonly IEventSourcingHandler _eventHandler;

    public EventSourcingInterceptor(
        IEventSourcingHandler eventHandler,
        ILogger<EventSourcingInterceptor> _)
    {
        _eventHandler = eventHandler;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
        {
            await _eventHandler.HandleEventsAsync(cancellationToken);
        }

        return result;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context != null)
        {
            _eventHandler.HandleEventsAsync().GetAwaiter().GetResult();
        }

        return result;
    }
}

/// <summary>
/// Event sourcing service for querying and replaying events
/// </summary>
public interface IEventSourcingService
{
    Task<EventSourcingAuditDto> GetAuditTrailAsync(Guid aggregateId, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventSourcingAuditDto>> GetAuditTrailByTypeAsync(string aggregateType, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<T?> GetAggregateAtVersionAsync<T>(Guid aggregateId, int version, CancellationToken cancellationToken = default) where T : class, new();
    Task<T?> GetAggregateAtDateAsync<T>(Guid aggregateId, DateTime asOf, CancellationToken cancellationToken = default) where T : class, new();
    Task CreateSnapshotAsync(Guid aggregateId, CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for audit trail
/// </summary>
public class EventSourcingAuditDto
{
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public List<EventAuditItemDto> Events { get; set; } = new();
    public int CurrentVersion { get; set; }
    public DateTime? LastModified { get; set; }
}

/// <summary>
/// DTO for individual audit events
/// </summary>
public class EventAuditItemDto
{
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Version { get; set; }
    public object? EventData { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Implementation of event sourcing service
/// </summary>
public class EventSourcingService : IEventSourcingService
{
    private readonly IEventStore _eventStore;
    private readonly IEventProjector _projector;
    private readonly ILogger<EventSourcingService> _logger;

    public EventSourcingService(
        IEventStore eventStore,
        IEventProjector projector,
        ILogger<EventSourcingService> logger)
    {
        _eventStore = eventStore;
        _projector = projector;
        _logger = logger;
    }

    public async Task<EventSourcingAuditDto> GetAuditTrailAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default)
    {
        var events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);
        var eventsList = events.ToList();

        var audit = new EventSourcingAuditDto
        {
            AggregateId = aggregateId,
            AggregateType = eventsList.FirstOrDefault()?.AggregateType ?? "Unknown",
            CurrentVersion = eventsList.Any() ? eventsList.Max(e => e.Version) : 0,
            LastModified = eventsList.Any() ? eventsList.Max(e => e.OccurredAt) : null,
            Events = eventsList.Select(e => new EventAuditItemDto
            {
                EventType = e.EventType,
                OccurredAt = e.OccurredAt,
                UserId = e.UserId,
                Version = e.Version,
                EventData = System.Text.Json.JsonSerializer.Deserialize<object>(e.EventData),
                Metadata = string.IsNullOrEmpty(e.Metadata)
                    ? null
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(e.Metadata)
            }).ToList()
        };

        return audit;
    }

    public async Task<IEnumerable<EventSourcingAuditDto>> GetAuditTrailByTypeAsync(
        string aggregateType,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var events = await _eventStore.GetEventsAsync(aggregateType, fromDate, toDate, cancellationToken);
        var groupedEvents = events.GroupBy(e => e.AggregateId);

        var audits = new List<EventSourcingAuditDto>();

        foreach (var group in groupedEvents)
        {
            var eventsList = group.ToList();
            var audit = new EventSourcingAuditDto
            {
                AggregateId = group.Key,
                AggregateType = aggregateType,
                CurrentVersion = eventsList.Max(e => e.Version),
                LastModified = eventsList.Max(e => e.OccurredAt),
                Events = eventsList.Select(e => new EventAuditItemDto
                {
                    EventType = e.EventType,
                    OccurredAt = e.OccurredAt,
                    UserId = e.UserId,
                    Version = e.Version,
                    EventData = System.Text.Json.JsonSerializer.Deserialize<object>(e.EventData)
                }).ToList()
            };

            audits.Add(audit);
        }

        return audits;
    }

    public async Task<T?> GetAggregateAtVersionAsync<T>(
        Guid aggregateId,
        int version,
        CancellationToken cancellationToken = default) where T : class, new()
    {
        return await _projector.ProjectAsync<T>(aggregateId, version, cancellationToken);
    }

    public async Task<T?> GetAggregateAtDateAsync<T>(
        Guid aggregateId,
        DateTime asOf,
        CancellationToken cancellationToken = default) where T : class, new()
    {
        var events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);
        var eventsBeforeDate = events.Where(e => e.OccurredAt <= asOf).ToList();

        if (eventsBeforeDate.Count == 0)
        {
            return null;
        }

        var maxVersion = eventsBeforeDate.Max(e => e.Version);
        return await _projector.ProjectAsync<T>(aggregateId, maxVersion, cancellationToken);
    }

    public async Task CreateSnapshotAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        // Get all events for the aggregate
        var events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);
        var eventsList = events.ToList();

        if (!eventsList.Any())
        {
            _logger.LogWarning("No events found for aggregate {AggregateId}", aggregateId);
            return;
        }

        // Determine aggregate type from events
        var aggregateType = eventsList.First().AggregateType;
        var latestVersion = eventsList.Max(e => e.Version);

        // Project current state
        // Note: This is a simplified version. In production, you'd use the actual aggregate type
        var projectedState = await _projector.ProjectAsync<object>(aggregateId, cancellationToken);

        if (projectedState == null)
        {
            _logger.LogWarning("Could not project state for aggregate {AggregateId}", aggregateId);
            return;
        }

        // Create and save snapshot
        var snapshot = new EventSnapshot
        {
            AggregateId = aggregateId,
            AggregateType = aggregateType,
            SnapshotData = System.Text.Json.JsonSerializer.Serialize(projectedState),
            Version = latestVersion
        };

        await _eventStore.SaveSnapshotAsync(snapshot, cancellationToken);

        _logger.LogInformation("Created snapshot for aggregate {AggregateId} at version {Version}",
            aggregateId, latestVersion);
    }
}

/// <summary>
/// Extension methods for configuring event sourcing
/// </summary>
public static class EventSourcingExtensions
{
    public static IServiceCollection AddEventSourcing(this IServiceCollection services)
    {
        services.AddScoped<IEventStore, EventStore>();
        services.AddScoped<IEventProjector, EventProjector>();
        services.AddScoped<IEventSourcingHandler, EventSourcingHandler>();
        services.AddScoped<IEventSourcingService, EventSourcingService>();
        services.AddScoped<EventSourcingInterceptor>();

        return services;
    }

    public static DbContextOptionsBuilder AddEventSourcingInterceptor(
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider)
    {
        var interceptor = serviceProvider.GetService<EventSourcingInterceptor>();
        if (interceptor != null)
        {
            optionsBuilder.AddInterceptors(interceptor);
        }

        return optionsBuilder;
    }
}