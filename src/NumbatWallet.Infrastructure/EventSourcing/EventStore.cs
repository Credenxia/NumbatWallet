using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Infrastructure.EventSourcing;

/// <summary>
/// Event store for persisting domain events
/// POA: Implementing event sourcing for complete audit trail
/// </summary>
public interface IEventStore
{
    Task SaveEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task SaveEventsAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
    Task<IEnumerable<StoredEvent>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StoredEvent>> GetEventsAsync(string aggregateType, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<StoredEvent>> GetAllEventsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<EventSnapshot?> GetLatestSnapshotAsync(Guid aggregateId, CancellationToken cancellationToken = default);
    Task SaveSnapshotAsync(EventSnapshot snapshot, CancellationToken cancellationToken = default);
}

/// <summary>
/// Stored event entity for persistence
/// </summary>
public class StoredEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AggregateId { get; set; }
    [MaxLength(200)]
    public string AggregateType { get; set; } = string.Empty;
    [MaxLength(200)]
    public string EventType { get; set; } = string.Empty;
    [MaxLength]
    public string EventData { get; set; } = string.Empty;
    [MaxLength]
    public string? Metadata { get; set; }
    public DateTime OccurredAt { get; set; }
    [MaxLength(200)]
    public string UserId { get; set; } = string.Empty;
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;
    public int Version { get; set; }
    public Guid CorrelationId { get; set; }
    [MaxLength(100)]
    public string? CausationId { get; set; }
}

/// <summary>
/// Event snapshot for optimizing replay
/// </summary>
public class EventSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AggregateId { get; set; }
    [MaxLength(200)]
    public string AggregateType { get; set; } = string.Empty;
    [MaxLength]
    public string SnapshotData { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;
}

/// <summary>
/// Event store implementation using EF Core
/// </summary>
public class EventStore : IEventStore
{
    private readonly NumbatWalletDbContext _context;
    private readonly ILogger<EventStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ITenantService? _tenantService;
    private readonly ICurrentUserService? _currentUserService;

    public EventStore(
        NumbatWalletDbContext context,
        ILogger<EventStore> logger)
    {
        _context = context;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Try to get tenant and user services from DbContext via reflection
        var tenantServiceProperty = context.GetType().GetField("_tenantService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        _tenantService = tenantServiceProperty?.GetValue(context) as ITenantService;

        var currentUserServiceProperty = context.GetType().GetField("_currentUserService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        _currentUserService = currentUserServiceProperty?.GetValue(context) as ICurrentUserService;
    }

    public async Task SaveEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var storedEvent = new StoredEvent
        {
            AggregateId = GetAggregateId(domainEvent),
            AggregateType = domainEvent.GetType().DeclaringType?.Name ?? "Unknown",
            EventType = domainEvent.GetType().Name,
            EventData = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _jsonOptions),
            OccurredAt = domainEvent.OccurredAt.DateTime,
            UserId = GetCurrentUserId(),
            TenantId = GetCurrentTenantId(),
            Version = await GetNextVersionAsync(GetAggregateId(domainEvent), cancellationToken),
            CorrelationId = GetCorrelationId()
        };

        _context.Set<StoredEvent>().Add(storedEvent);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Event {EventType} saved for aggregate {AggregateId}",
            storedEvent.EventType, storedEvent.AggregateId);
    }

    public async Task SaveEventsAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        var storedEvents = new List<StoredEvent>();
        var eventsList = events.ToList();
        var aggregateVersions = new Dictionary<Guid, int>();

        foreach (var domainEvent in eventsList)
        {
            var aggregateId = GetAggregateId(domainEvent);

            // Get or initialize version for this aggregate
            if (!aggregateVersions.ContainsKey(aggregateId))
            {
                aggregateVersions[aggregateId] = await GetNextVersionAsync(aggregateId, cancellationToken);
            }

            var storedEvent = new StoredEvent
            {
                AggregateId = aggregateId,
                AggregateType = domainEvent.GetType().DeclaringType?.Name ?? "Unknown",
                EventType = domainEvent.GetType().Name,
                EventData = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _jsonOptions),
                OccurredAt = domainEvent.OccurredAt.DateTime,
                UserId = GetCurrentUserId(),
                TenantId = GetCurrentTenantId(),
                Version = aggregateVersions[aggregateId]++,
                CorrelationId = GetCorrelationId()
            };

            storedEvents.Add(storedEvent);
        }

        _context.Set<StoredEvent>().AddRange(storedEvents);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved {Count} events", storedEvents.Count);
    }

    public async Task<IEnumerable<StoredEvent>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<StoredEvent>()
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StoredEvent>> GetEventsAsync(
        string aggregateType,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<StoredEvent>()
            .Where(e => e.AggregateType == aggregateType &&
                       e.OccurredAt >= fromDate &&
                       e.OccurredAt <= toDate)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StoredEvent>> GetAllEventsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<StoredEvent>()
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<EventSnapshot?> GetLatestSnapshotAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<EventSnapshot>()
            .Where(s => s.AggregateId == aggregateId)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveSnapshotAsync(EventSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        snapshot.CreatedAt = DateTime.UtcNow;
        snapshot.TenantId = GetCurrentTenantId();

        _context.Set<EventSnapshot>().Add(snapshot);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Snapshot saved for aggregate {AggregateId} at version {Version}",
            snapshot.AggregateId, snapshot.Version);
    }

    private async Task<int> GetNextVersionAsync(Guid aggregateId, CancellationToken cancellationToken)
    {
        var lastVersion = await _context.Set<StoredEvent>()
            .Where(e => e.AggregateId == aggregateId)
            .OrderByDescending(e => e.Version)
            .Select(e => e.Version)
            .FirstOrDefaultAsync(cancellationToken);

        return lastVersion + 1;
    }

    private string GetCurrentUserId()
    {
        // Try to get from current user service
        if (_currentUserService != null)
        {
            return _currentUserService.UserId;
        }

        return "system";
    }

    private string GetCurrentTenantId()
    {
        // Try to get from tenant service
        if (_tenantService != null)
        {
            return _tenantService.TenantId.ToString();
        }

        return "default";
    }

    private Guid GetCorrelationId()
    {
        // In a real implementation, get from HttpContext or generate
        return Guid.NewGuid();
    }

    private Guid GetAggregateId(IDomainEvent domainEvent)
    {
        // Try to get AggregateId from event properties using reflection
        var aggregateIdProperty = domainEvent.GetType().GetProperty("AggregateId");
        if (aggregateIdProperty != null && aggregateIdProperty.PropertyType == typeof(Guid))
        {
            return (Guid)aggregateIdProperty.GetValue(domainEvent)!;
        }

        // Try EntityId as fallback
        var entityIdProperty = domainEvent.GetType().GetProperty("EntityId");
        if (entityIdProperty != null && entityIdProperty.PropertyType == typeof(Guid))
        {
            return (Guid)entityIdProperty.GetValue(domainEvent)!;
        }

        // Default to EventId if no aggregate ID found
        return domainEvent.EventId;
    }
}

/// <summary>
/// Event projector for rebuilding state from events
/// </summary>
public interface IEventProjector
{
    Task<T?> ProjectAsync<T>(Guid aggregateId, CancellationToken cancellationToken = default) where T : class, new();
    Task<T?> ProjectAsync<T>(Guid aggregateId, int toVersion, CancellationToken cancellationToken = default) where T : class, new();
    Task ReplayEventsAsync(Guid aggregateId, Action<IDomainEvent> handler, CancellationToken cancellationToken = default);
}

/// <summary>
/// Event projector implementation
/// </summary>
public class EventProjector : IEventProjector
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<EventProjector> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public EventProjector(
        IEventStore eventStore,
        ILogger<EventProjector> logger)
    {
        _eventStore = eventStore;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> ProjectAsync<T>(Guid aggregateId, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        // Check for snapshot first
        var snapshot = await _eventStore.GetLatestSnapshotAsync(aggregateId, cancellationToken);

        T? aggregate;
        int fromVersion = 0;

        if (snapshot != null)
        {
            aggregate = JsonSerializer.Deserialize<T>(snapshot.SnapshotData, _jsonOptions);
            fromVersion = snapshot.Version + 1;
            _logger.LogDebug("Loading from snapshot at version {Version}", snapshot.Version);
        }
        else
        {
            aggregate = new T();
        }

        // Apply events after snapshot
        var events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);
        var eventsToApply = events.Where(e => e.Version >= fromVersion).OrderBy(e => e.Version);

        foreach (var storedEvent in eventsToApply)
        {
            var eventType = Type.GetType(storedEvent.EventType) ?? typeof(object);
            var domainEvent = JsonSerializer.Deserialize(storedEvent.EventData, eventType, _jsonOptions) as IDomainEvent;

            if (domainEvent != null && aggregate != null)
            {
                ApplyEvent(aggregate, domainEvent);
            }
        }

        return aggregate;
    }

    public async Task<T?> ProjectAsync<T>(Guid aggregateId, int toVersion, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        var aggregate = new T();

        var events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);
        var eventsToApply = events.Where(e => e.Version <= toVersion).OrderBy(e => e.Version);

        foreach (var storedEvent in eventsToApply)
        {
            var eventType = Type.GetType(storedEvent.EventType) ?? typeof(object);
            var domainEvent = JsonSerializer.Deserialize(storedEvent.EventData, eventType, _jsonOptions) as IDomainEvent;

            if (domainEvent != null && aggregate != null)
            {
                ApplyEvent(aggregate, domainEvent);
            }
        }

        return aggregate;
    }

    public async Task ReplayEventsAsync(
        Guid aggregateId,
        Action<IDomainEvent> handler,
        CancellationToken cancellationToken = default)
    {
        var events = (await _eventStore.GetEventsAsync(aggregateId, cancellationToken)).ToList();

        foreach (var storedEvent in events.OrderBy(e => e.Version))
        {
            var eventType = Type.GetType(storedEvent.EventType) ?? typeof(object);
            var domainEvent = JsonSerializer.Deserialize(storedEvent.EventData, eventType, _jsonOptions) as IDomainEvent;

            if (domainEvent != null)
            {
                handler(domainEvent);
            }
        }

        _logger.LogInformation("Replayed {Count} events for aggregate {AggregateId}",
            events.Count, aggregateId);
    }

    private void ApplyEvent<T>(T aggregate, IDomainEvent domainEvent) where T : class
    {
        // Use reflection to find and invoke Apply method
        var applyMethod = aggregate.GetType()
            .GetMethod("Apply", new[] { domainEvent.GetType() });

        if (applyMethod != null)
        {
            applyMethod.Invoke(aggregate, new object[] { domainEvent });
        }
        else
        {
            _logger.LogWarning("No Apply method found for event type {EventType} on {AggregateType}",
                domainEvent.GetType().Name, aggregate.GetType().Name);
        }
    }
}

/// <summary>
/// Event sourcing configuration
/// </summary>
public static class EventSourcingConfiguration
{
    public static void ConfigureEventSourcing(this ModelBuilder modelBuilder)
    {
        // Configure StoredEvent entity
        modelBuilder.Entity<StoredEvent>(entity =>
        {
            entity.ToTable("EventStore");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.AggregateId);
            entity.HasIndex(e => e.AggregateType);
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.AggregateId, e.Version }).IsUnique();

            entity.Property(e => e.EventData).IsRequired();
            entity.Property(e => e.EventType).HasMaxLength(255).IsRequired();
            entity.Property(e => e.AggregateType).HasMaxLength(255).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.TenantId).HasMaxLength(50).IsRequired();
        });

        // Configure EventSnapshot entity
        modelBuilder.Entity<EventSnapshot>(entity =>
        {
            entity.ToTable("EventSnapshots");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.AggregateId, e.Version });
            entity.HasIndex(e => e.TenantId);

            entity.Property(e => e.SnapshotData).IsRequired();
            entity.Property(e => e.AggregateType).HasMaxLength(255).IsRequired();
            entity.Property(e => e.TenantId).HasMaxLength(50).IsRequired();
        });
    }
}