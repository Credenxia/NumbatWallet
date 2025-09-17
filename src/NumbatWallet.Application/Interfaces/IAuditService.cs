using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for logging and managing audit trails for data access
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs an audit entry for data access
    /// </summary>
    /// <param name="auditEntry">The audit entry to log</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogAccessAsync(object auditEntry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a sensitive data unmask operation
    /// </summary>
    /// <param name="entityType">The entity type</param>
    /// <param name="entityId">The entity ID</param>
    /// <param name="fieldName">The field that was unmasked</param>
    /// <param name="classification">The data classification</param>
    /// <param name="reason">The reason for unmasking</param>
    /// <param name="userId">The user who performed the unmask</param>
    /// <param name="duration">The unmask duration in seconds</param>
    Task LogUnmaskOperationAsync(
        string entityType,
        string entityId,
        string fieldName,
        DataClassification classification,
        string reason,
        string userId,
        int duration);

    /// <summary>
    /// Gets audit logs for an entity
    /// </summary>
    /// <param name="entityType">The entity type</param>
    /// <param name="entityId">The entity ID</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <returns>List of audit entries</returns>
    Task<IEnumerable<AuditLogEntry>> GetAuditLogsAsync(
        string entityType,
        string entityId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null);

    /// <summary>
    /// Gets unmask audit logs for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <returns>List of unmask audit entries</returns>
    Task<IEnumerable<UnmaskAuditEntry>> GetUnmaskAuditLogsAsync(
        string userId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null);

    /// <summary>
    /// Gets statistics on sensitive data access
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="period">The time period for statistics</param>
    /// <returns>Access statistics</returns>
    Task<SensitiveDataAccessStats> GetAccessStatisticsAsync(
        Guid tenantId,
        TimePeriod period);
}

/// <summary>
/// Represents an audit log entry
/// </summary>
public class AuditLogEntry
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public DataClassification MaxClassification { get; set; }
    public Dictionary<string, object> ChangedFields { get; set; } = new();
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// Represents an unmask operation audit entry
/// </summary>
public class UnmaskAuditEntry
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public DataClassification Classification { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public DateTimeOffset UnmaskedAt { get; set; }
    public int DurationSeconds { get; set; }
    public bool RequiredMfa { get; set; }
    public string? ApprovedBy { get; set; }
}

/// <summary>
/// Statistics for sensitive data access
/// </summary>
public class SensitiveDataAccessStats
{
    public int TotalAccessCount { get; set; }
    public int UnmaskOperationCount { get; set; }
    public Dictionary<DataClassification, int> AccessByClassification { get; set; } = new();
    public Dictionary<string, int> TopAccessedEntities { get; set; } = new();
    public Dictionary<string, int> TopUsers { get; set; } = new();
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
}

/// <summary>
/// Time period for statistics
/// </summary>
public enum TimePeriod
{
    Day,
    Week,
    Month,
    Quarter,
    Year
}