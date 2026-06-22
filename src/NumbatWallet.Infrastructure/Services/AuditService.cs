using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;
using System.Text.Json;

namespace NumbatWallet.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;
    private readonly NumbatWalletDbContext _dbContext;
    private readonly ICurrentTenantService _currentTenantService;

    public AuditService(
        ILogger<AuditService> logger,
        NumbatWalletDbContext dbContext,
        ICurrentTenantService currentTenantService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _currentTenantService = currentTenantService;
    }

    public async Task LogAccessAsync(object auditEntry, CancellationToken cancellationToken = default)
    {
        if (auditEntry is AuditLogEntry entry)
        {
            try
            {
                // Create domain entity from application DTO
                var audit = new AuditLog(
                    entry.UserId,
                    entry.Action,
                    entry.EntityType,
                    entry.EntityId,
                    entry.TenantId,
                    AuditEventType.Read);

                // Set change values (convert dictionary to JSON)
                var oldValues = entry.ChangedFields.Any()
                    ? JsonSerializer.Serialize(entry.ChangedFields)
                    : null;
                audit.SetChangeValues(oldValues, null);
                audit.SetMaxClassification(entry.MaxClassification);

                // Persist to database
                await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Audit: {Action} on {EntityType}:{EntityId} by {UserId} - persisted to database",
                    entry.Action, entry.EntityType, entry.EntityId, entry.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist audit log for {EntityType}:{EntityId}",
                    entry.EntityType, entry.EntityId);
                // Don't throw - audit logging should not break application flow
            }
        }
    }

    public async Task LogUnmaskOperationAsync(
        string entityType,
        string entityId,
        string fieldName,
        DataClassification classification,
        string reason,
        string userId,
        int duration)
    {
        try
        {
            // Get tenant ID from the current tenant context
            if (!Guid.TryParse(_currentTenantService.TenantId, out var tenantId) || tenantId == Guid.Empty)
            {
                _logger.LogWarning("Cannot create audit record without a valid tenant ID");
                throw new InvalidOperationException("A valid tenant ID is required for audit logging.");
            }

            // Create unmask audit record
            var unmaskAudit = new UnmaskAudit(
                entityType,
                entityId,
                fieldName,
                classification,
                reason,
                userId,
                tenantId,
                duration);

            // Persist to database
            await _dbContext.UnmaskAudits.AddAsync(unmaskAudit, cancellationToken: default);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Unmask: {EntityType}:{EntityId}.{FieldName} by {UserId} for {Duration}s - persisted to database",
                entityType, entityId, fieldName, userId, duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist unmask audit for {EntityType}:{EntityId}.{FieldName}",
                entityType, entityId, fieldName);
            // Don't throw - audit logging should not break application flow
        }
    }

    public async Task<IEnumerable<AuditLogEntry>> GetAuditLogsAsync(
        string entityType,
        string entityId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        try
        {
            var query = _dbContext.AuditLogs
                .Where(a => a.EntityType == entityType && a.EntityId == entityId);

            if (startDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt <= endDate.Value);
            }

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(1000) // Limit to prevent performance issues
                .ToListAsync();

            // Map domain entities to application DTOs
            return logs.Select(a => new AuditLogEntry
            {
                Id = a.Id,
                EntityType = a.EntityType ?? string.Empty,
                EntityId = a.EntityId ?? string.Empty,
                Action = a.Action,
                UserId = a.UserId,
                TenantId = a.TenantId,
                Timestamp = a.CreatedAt,
                MaxClassification = a.MaxClassification,
                ChangedFields = !string.IsNullOrEmpty(a.OldValues)
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(a.OldValues) ?? new Dictionary<string, object>()
                    : new Dictionary<string, object>()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit logs for {EntityType}:{EntityId}",
                entityType, entityId);
            return new List<AuditLogEntry>();
        }
    }

    public async Task<IEnumerable<UnmaskAuditEntry>> GetUnmaskAuditLogsAsync(
        string userId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        try
        {
            var query = _dbContext.UnmaskAudits
                .Where(u => u.UserId == userId);

            if (startDate.HasValue)
            {
                query = query.Where(u => u.UnmaskedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(u => u.UnmaskedAt <= endDate.Value);
            }

            var unmaskLogs = await query
                .OrderByDescending(u => u.UnmaskedAt)
                .Take(1000) // Limit to prevent performance issues
                .ToListAsync();

            // Map domain entities to application DTOs
            return unmaskLogs.Select(u => new UnmaskAuditEntry
            {
                Id = u.Id,
                EntityType = u.EntityType,
                EntityId = u.EntityId,
                FieldName = u.FieldName,
                Classification = u.Classification,
                Reason = u.Reason,
                UserId = u.UserId,
                TenantId = u.TenantId,
                UnmaskedAt = u.UnmaskedAt,
                DurationSeconds = u.DurationSeconds,
                RequiredMfa = false, // TODO: Add MFA tracking to UnmaskAudit entity
                ApprovedBy = u.ApprovalReference
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve unmask audit logs for user {UserId}", userId);
            return new List<UnmaskAuditEntry>();
        }
    }

    public async Task<SensitiveDataAccessStats> GetAccessStatisticsAsync(
        Guid tenantId,
        TimePeriod period)
    {
        try
        {
            // Calculate period boundaries
            var (periodStart, periodEnd) = GetPeriodBoundaries(period);

            // Query audit logs for the period
            var auditLogs = await _dbContext.AuditLogs
                .Where(a => a.TenantId == tenantId &&
                           a.CreatedAt >= periodStart &&
                           a.CreatedAt <= periodEnd)
                .ToListAsync();

            // Query unmask operations for the period
            var unmaskLogs = await _dbContext.UnmaskAudits
                .Where(u => u.TenantId == tenantId &&
                           u.UnmaskedAt >= periodStart &&
                           u.UnmaskedAt <= periodEnd)
                .ToListAsync();

            // Calculate statistics
            var stats = new SensitiveDataAccessStats
            {
                TotalAccessCount = auditLogs.Count,
                UnmaskOperationCount = unmaskLogs.Count,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,

                // Access by classification
                AccessByClassification = auditLogs
                    .GroupBy(a => a.MaxClassification)
                    .ToDictionary(g => g.Key, g => g.Count()),

                // Top accessed entities
                TopAccessedEntities = auditLogs
                    .Where(a => a.EntityType != null && a.EntityId != null)
                    .GroupBy(a => $"{a.EntityType}:{a.EntityId}")
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count()),

                // Top users
                TopUsers = auditLogs
                    .GroupBy(a => a.UserId)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate access statistics for tenant {TenantId}", tenantId);
            return new SensitiveDataAccessStats
            {
                TotalAccessCount = 0,
                UnmaskOperationCount = 0,
                PeriodStart = DateTimeOffset.UtcNow.AddDays(-1),
                PeriodEnd = DateTimeOffset.UtcNow
            };
        }
    }

    private static (DateTimeOffset start, DateTimeOffset end) GetPeriodBoundaries(TimePeriod period)
    {
        var now = DateTimeOffset.UtcNow;
        var start = period switch
        {
            TimePeriod.Day => now.AddDays(-1),
            TimePeriod.Week => now.AddDays(-7),
            TimePeriod.Month => now.AddMonths(-1),
            TimePeriod.Quarter => now.AddMonths(-3),
            TimePeriod.Year => now.AddYears(-1),
            _ => now.AddDays(-1)
        };
        return (start, now);
    }
}
