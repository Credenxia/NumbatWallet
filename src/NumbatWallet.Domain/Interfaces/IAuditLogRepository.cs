using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Domain.Interfaces;

/// <summary>
/// Repository interface for AuditLog entity
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// Adds a new audit log entry
    /// </summary>
    Task<AuditLog> AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit log by ID
    /// </summary>
    Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific entity
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByEntityAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs within a date range
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a tenant within a date range
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByTenantAndDateRangeAsync(
        Guid tenantId,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default);
}