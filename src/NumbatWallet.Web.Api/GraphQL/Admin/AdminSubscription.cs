using HotChocolate.Subscriptions;
using System.Runtime.CompilerServices;

namespace NumbatWallet.Web.Api.GraphQL.Admin;

/// <summary>
/// Admin GraphQL subscriptions for real-time monitoring
/// POA: Issue #153 - Admin GraphQL API
/// </summary>
[ExtendObjectType("Subscription")]
[Authorize(Policy = "AdminOnly")]
public class AdminSubscription
{
    /// <summary>
    /// Subscribe to system metrics updates
    /// </summary>
    [Subscribe]
    [GraphQLDescription("Real-time system metrics updates")]
    public async IAsyncEnumerable<MetricsUpdateDto> SystemMetrics(
        [Service] ITopicEventReceiver eventReceiver,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var stream = await eventReceiver.SubscribeAsync<MetricsUpdateDto>(
            "SystemMetrics",
            cancellationToken);

        await foreach (var metrics in stream.ReadEventsAsync().WithCancellation(cancellationToken))
        {
            yield return metrics;
        }
    }

    /// <summary>
    /// Subscribe to audit log events
    /// </summary>
    [Subscribe]
    [GraphQLDescription("Real-time audit log entries")]
    public async IAsyncEnumerable<AuditLogEntryDto> AuditLogAdded(
        [Service] ITopicEventReceiver eventReceiver,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var stream = await eventReceiver.SubscribeAsync<AuditLogEntryDto>(
            "AuditLog",
            cancellationToken);

        await foreach (var entry in stream.ReadEventsAsync().WithCancellation(cancellationToken))
        {
            yield return entry;
        }
    }

    /// <summary>
    /// Subscribe to batch operation progress
    /// </summary>
    [Subscribe]
    [Topic("BatchOperation_{jobId}")]
    [GraphQLDescription("Track progress of a specific batch operation")]
    public BatchOperationProgressDto BatchOperationProgress(
        [EventMessage] BatchOperationProgressDto progress,
        string jobId)
    {
        return progress;
    }

    /// <summary>
    /// Subscribe to backup progress
    /// </summary>
    [Subscribe]
    [Topic("BackupProgress_{jobId}")]
    [GraphQLDescription("Track progress of a backup operation")]
    public BackupProgressDto BackupProgress(
        [EventMessage] BackupProgressDto progress,
        string jobId)
    {
        return progress;
    }

    /// <summary>
    /// Subscribe to restore progress
    /// </summary>
    [Subscribe]
    [Topic("RestoreProgress_{jobId}")]
    [GraphQLDescription("Track progress of a restore operation")]
    public RestoreProgressDto RestoreProgress(
        [EventMessage] RestoreProgressDto progress,
        string jobId)
    {
        return progress;
    }

    /// <summary>
    /// Subscribe to system alerts
    /// </summary>
    [Subscribe]
    [GraphQLDescription("Real-time system alerts and warnings")]
    public async IAsyncEnumerable<SystemAlertDto> SystemAlert(
        [Service] ITopicEventReceiver eventReceiver,
        AlertSeverity? minSeverity,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var stream = await eventReceiver.SubscribeAsync<SystemAlertDto>(
            "SystemAlerts",
            cancellationToken);

        await foreach (var alert in stream.ReadEventsAsync().WithCancellation(cancellationToken))
        {
            if (minSeverity == null || alert.Severity >= minSeverity)
            {
                yield return alert;
            }
        }
    }

    /// <summary>
    /// Subscribe to tenant activity
    /// </summary>
    [Subscribe]
    [GraphQLDescription("Monitor tenant activity in real-time")]
    public async IAsyncEnumerable<TenantActivityDto> TenantActivity(
        [Service] ITopicEventReceiver eventReceiver,
        string? tenantId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var topic = string.IsNullOrEmpty(tenantId)
            ? "TenantActivity"
            : $"TenantActivity_{tenantId}";

        var stream = await eventReceiver.SubscribeAsync<TenantActivityDto>(
            topic,
            cancellationToken);

        await foreach (var activity in stream.ReadEventsAsync().WithCancellation(cancellationToken))
        {
            yield return activity;
        }
    }

    /// <summary>
    /// Subscribe to database performance metrics
    /// </summary>
    [Subscribe]
    [GraphQLDescription("Real-time database performance metrics")]
    public async IAsyncEnumerable<DatabaseMetricsDto> DatabaseMetrics(
        [Service] ITopicEventReceiver eventReceiver,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var stream = await eventReceiver.SubscribeAsync<DatabaseMetricsDto>(
            "DatabaseMetrics",
            cancellationToken);

        await foreach (var metrics in stream.ReadEventsAsync().WithCancellation(cancellationToken))
        {
            yield return metrics;
        }
    }

    /// <summary>
    /// Subscribe to API usage metrics
    /// </summary>
    [Subscribe]
    [GraphQLDescription("Real-time API usage statistics")]
    public async IAsyncEnumerable<ApiUsageMetricsDto> ApiUsageMetrics(
        [Service] ITopicEventReceiver eventReceiver,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var stream = await eventReceiver.SubscribeAsync<ApiUsageMetricsDto>(
            "ApiUsage",
            cancellationToken);

        await foreach (var usage in stream.ReadEventsAsync().WithCancellation(cancellationToken))
        {
            yield return usage;
        }
    }

    /// <summary>
    /// Subscribe to security events
    /// </summary>
    [Subscribe]
    [GraphQLDescription("Monitor security events in real-time")]
    [Authorize(Policy = "SuperAdmin")]
    public async IAsyncEnumerable<SecurityEventDto> SecurityEvents(
        [Service] ITopicEventReceiver eventReceiver,
        SecurityEventType? eventType,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var stream = await eventReceiver.SubscribeAsync<SecurityEventDto>(
            "SecurityEvents",
            cancellationToken);

        await foreach (var securityEvent in stream.ReadEventsAsync().WithCancellation(cancellationToken))
        {
            if (eventType == null || securityEvent.EventType == eventType)
            {
                yield return securityEvent;
            }
        }
    }
}

// DTO types for subscriptions
public class MetricsUpdateDto
{
    public DateTime Timestamp { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double DiskUsage { get; set; }
    public int ActiveConnections { get; set; }
    public int RequestsPerSecond { get; set; }
    public double AverageResponseTime { get; set; }
    public Dictionary<string, double> CustomMetrics { get; set; } = new();
}

public class AuditLogEntryDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class BatchOperationProgressDto
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double PercentComplete { get; set; }
    public string? CurrentOperation { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BackupProgressDto
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public long BytesProcessed { get; set; }
    public long TotalBytes { get; set; }
    public double PercentComplete { get; set; }
    public string? CurrentTable { get; set; }
    public DateTime UpdatedAt { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; }
}

public class RestoreProgressDto
{
    public string JobId { get; set; } = string.Empty;
    public string BackupId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public long BytesRestored { get; set; }
    public long TotalBytes { get; set; }
    public double PercentComplete { get; set; }
    public int TablesRestored { get; set; }
    public int TotalTables { get; set; }
    public string? CurrentTable { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SystemAlertDto
{
    public string Id { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTime OccurredAt { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class TenantActivityDto
{
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class DatabaseMetricsDto
{
    public DateTime Timestamp { get; set; }
    public int ActiveConnections { get; set; }
    public int IdleConnections { get; set; }
    public long QueriesPerSecond { get; set; }
    public double AverageQueryTime { get; set; }
    public long SlowQueries { get; set; }
    public long DeadlockCount { get; set; }
    public double CacheHitRatio { get; set; }
    public long TableSize { get; set; }
    public long IndexSize { get; set; }
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}

public class ApiUsageMetricsDto
{
    public DateTime Timestamp { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public double AverageResponseTime { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public Dictionary<int, int> StatusCodeDistribution { get; set; } = new();
    public List<string> TopUsers { get; set; } = new();
    public List<string> TopTenants { get; set; } = new();
}

public class SecurityEventDto
{
    public string Id { get; set; } = string.Empty;
    public SecurityEventType EventType { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTime OccurredAt { get; set; }
}

// Enum types
public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public enum SecurityEventType
{
    Login,
    Logout,
    FailedLogin,
    PasswordChange,
    PermissionChange,
    SuspiciousActivity,
    DataExport,
    ConfigurationChange,
    KeyRotation,
    UnauthorizedAccess
}
