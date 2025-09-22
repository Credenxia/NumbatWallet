using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.SharedKernel.Models;

namespace NumbatWallet.Web.Api.GraphQL.Admin;

/// <summary>
/// Admin GraphQL queries for system management
/// POA: Issue #153 - Admin GraphQL API
/// </summary>
[ExtendObjectType("Query")]
[Authorize(Policy = "AdminOnly")]
public class AdminQuery
{
    /// <summary>
    /// Get system health status
    /// </summary>
    [GraphQLDescription("Get current system health and component status")]
    public async Task<SystemHealthDto> GetSystemHealth(
        [Service] IHealthCheckService healthService,
        CancellationToken cancellationToken = default)
    {
        return await healthService.GetSystemHealthAsync(cancellationToken);
    }

    /// <summary>
    /// Get system metrics snapshot
    /// </summary>
    [GraphQLDescription("Get system metrics for the specified time range")]
    public async Task<MetricsSnapshotDto> GetMetrics(
        [Service] IStatisticsService statisticsService,
        TimeRangeInput timeRange,
        CancellationToken cancellationToken = default)
    {
        var from = DateTime.UtcNow.Add(-timeRange.GetTimeSpan());
        var to = DateTime.UtcNow;

        return await statisticsService.GetMetricsSnapshotAsync(from, to, cancellationToken);
    }

    /// <summary>
    /// Get audit logs with filtering
    /// </summary>
    [GraphQLDescription("Query audit logs with filtering and pagination")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public IQueryable<AuditLogDto> GetAuditLogs(
        [Service] NumbatWalletDbContext context,
        AuditLogFilterInput? filter = null)
    {
        var query = context.AuditLogs.AsQueryable();

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.UserId))
                query = query.Where(a => a.UserId == filter.UserId);

            if (!string.IsNullOrEmpty(filter.Action))
                query = query.Where(a => a.Action.Contains(filter.Action));

            if (filter.From.HasValue)
                query = query.Where(a => a.CreatedAt >= filter.From.Value);

            if (filter.To.HasValue)
                query = query.Where(a => a.CreatedAt <= filter.To.Value);

            if (filter.EventType.HasValue)
                query = query.Where(a => a.EventType == filter.EventType.Value);
        }

        return query.OrderByDescending(a => a.CreatedAt)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                EventType = a.EventType,
                CreatedAt = a.CreatedAt
            });
    }

    /// <summary>
    /// Get all tenants with filtering
    /// </summary>
    [GraphQLDescription("Query all tenants with filtering and pagination")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public IQueryable<TenantDto> GetTenants(
        [Service] ITenantService tenantService,
        TenantFilterInput? filter = null)
    {
        var tenants = tenantService.GetAllTenants();

        if (filter != null)
        {
            if (filter.Status.HasValue)
                tenants = tenants.Where(t => t.Status == filter.Status.Value);

            if (!string.IsNullOrEmpty(filter.SearchTerm))
                tenants = tenants.Where(t =>
                    t.Name.Contains(filter.SearchTerm) ||
                    t.Identifier.Contains(filter.SearchTerm));

            if (filter.CreatedAfter.HasValue)
                tenants = tenants.Where(t => t.CreatedAt >= filter.CreatedAfter.Value);
        }

        return tenants.AsQueryable();
    }

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    [GraphQLDescription("Get detailed information about a specific tenant")]
    public async Task<TenantDto?> GetTenant(
        [Service] ITenantService tenantService,
        string id,
        CancellationToken cancellationToken = default)
    {
        return await tenantService.GetTenantByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// Get admin users
    /// </summary>
    [GraphQLDescription("Query admin users with filtering and pagination")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public IQueryable<AdminUserDto> GetAdminUsers(
        [Service] NumbatWalletDbContext context,
        UserFilterInput? filter = null)
    {
        var query = context.Users
            .Where(u => u.Roles.Any(r => r.Name == "Admin" || r.Name == "SuperAdmin"))
            .AsQueryable();

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(u =>
                    u.Email.Contains(filter.SearchTerm) ||
                    u.FirstName.Contains(filter.SearchTerm) ||
                    u.LastName.Contains(filter.SearchTerm));
            }

            if (filter.IsActive.HasValue)
                query = query.Where(u => u.IsActive == filter.IsActive.Value);

            if (!string.IsNullOrEmpty(filter.Role))
                query = query.Where(u => u.Roles.Any(r => r.Name == filter.Role));
        }

        return query.Select(u => new AdminUserDto
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Roles = u.Roles.Select(r => r.Name).ToList(),
            IsActive = u.IsActive,
            LastLogin = u.LastLogin,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        });
    }

    /// <summary>
    /// Get backup history
    /// </summary>
    [GraphQLDescription("Query backup history with pagination")]
    [UsePaging]
    [UseSorting]
    public async Task<IQueryable<BackupDto>> GetBackups(
        [Service] IBackupService backupService,
        CancellationToken cancellationToken = default)
    {
        var backups = await backupService.GetBackupHistoryAsync(cancellationToken);
        return backups.AsQueryable();
    }

    /// <summary>
    /// Get backup status
    /// </summary>
    [GraphQLDescription("Get the status of a specific backup operation")]
    public async Task<BackupStatusDto?> GetBackupStatus(
        [Service] IBackupService backupService,
        string id,
        CancellationToken cancellationToken = default)
    {
        return await backupService.GetBackupStatusAsync(id, cancellationToken);
    }

    /// <summary>
    /// Get feature flags
    /// </summary>
    [GraphQLDescription("Get all feature flags and their current states")]
    public async Task<List<FeatureFlagDto>> GetFeatureFlags(
        [Service] IFeatureFlagService featureFlagService,
        CancellationToken cancellationToken = default)
    {
        return await featureFlagService.GetAllFlagsAsync(cancellationToken);
    }

    /// <summary>
    /// Get system configurations
    /// </summary>
    [GraphQLDescription("Get system configurations for the specified environment")]
    public async Task<List<ConfigurationDto>> GetConfigurations(
        [Service] IConfigurationService configService,
        string environment,
        CancellationToken cancellationToken = default)
    {
        return await configService.GetConfigurationsAsync(environment, cancellationToken);
    }

    /// <summary>
    /// Generate a report
    /// </summary>
    [GraphQLDescription("Generate a system report of the specified type")]
    public async Task<ReportDto> GenerateReport(
        [Service] IReportingService reportingService,
        ReportType type,
        ReportParametersInput parameters,
        CancellationToken cancellationToken = default)
    {
        return await reportingService.GenerateReportAsync(
            type,
            parameters.ToReportParameters(),
            cancellationToken);
    }

    /// <summary>
    /// Get scheduled reports
    /// </summary>
    [GraphQLDescription("Get all scheduled reports")]
    public async Task<List<ScheduledReportDto>> GetScheduledReports(
        [Service] IReportingService reportingService,
        CancellationToken cancellationToken = default)
    {
        return await reportingService.GetScheduledReportsAsync(cancellationToken);
    }

    /// <summary>
    /// Get database statistics
    /// </summary>
    [GraphQLDescription("Get database usage and performance statistics")]
    public async Task<DatabaseStatsDto> GetDatabaseStats(
        [Service] NumbatWalletDbContext context,
        CancellationToken cancellationToken = default)
    {
        var stats = new DatabaseStatsDto
        {
            TotalWallets = await context.Wallets.CountAsync(cancellationToken),
            TotalCredentials = await context.Credentials.CountAsync(cancellationToken),
            TotalPersons = await context.Persons.CountAsync(cancellationToken),
            TotalOrganizations = await context.Organizations.CountAsync(cancellationToken),
            DatabaseSizeMB = await GetDatabaseSizeAsync(context, cancellationToken),
            LastBackup = await GetLastBackupTimeAsync(context, cancellationToken)
        };

        return stats;
    }

    private async Task<decimal> GetDatabaseSizeAsync(
        NumbatWalletDbContext context,
        CancellationToken cancellationToken)
    {
        // PostgreSQL query to get database size
        var sql = @"
            SELECT pg_database_size(current_database()) / 1024.0 / 1024.0 AS size_mb";

        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        await context.Database.OpenConnectionAsync(cancellationToken);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null ? Convert.ToDecimal(result) : 0;
    }

    private async Task<DateTime?> GetLastBackupTimeAsync(
        NumbatWalletDbContext context,
        CancellationToken cancellationToken)
    {
        // Query backup metadata table if it exists
        return await context.Set<BackupMetadata>()
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => b.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

// Input types
public class TimeRangeInput
{
    public TimeRangeType Type { get; set; }
    public int? CustomHours { get; set; }

    public TimeSpan GetTimeSpan()
    {
        return Type switch
        {
            TimeRangeType.LastHour => TimeSpan.FromHours(1),
            TimeRangeType.Last24Hours => TimeSpan.FromHours(24),
            TimeRangeType.Last7Days => TimeSpan.FromDays(7),
            TimeRangeType.Last30Days => TimeSpan.FromDays(30),
            TimeRangeType.Custom => TimeSpan.FromHours(CustomHours ?? 24),
            _ => TimeSpan.FromHours(24)
        };
    }
}

public enum TimeRangeType
{
    LastHour,
    Last24Hours,
    Last7Days,
    Last30Days,
    Custom
}

public class AuditLogFilterInput
{
    public string? UserId { get; set; }
    public string? Action { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public AuditEventType? EventType { get; set; }
}

public class TenantFilterInput
{
    public TenantStatus? Status { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? CreatedAfter { get; set; }
}

public class UserFilterInput
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public string? Role { get; set; }
}

// ReportParametersInput moved to AdminMutation.cs to avoid duplication

// DTOs
public class SystemHealthDto
{
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, ComponentHealthDto> Components { get; set; } = new();
    public DateTime CheckedAt { get; set; }
}

public class ComponentHealthDto
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

public class MetricsSnapshotDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public Dictionary<string, decimal> Metrics { get; set; } = new();
    public List<TimeSeriesDataPoint> TimeSeries { get; set; } = new();
}

public class TimeSeriesDataPoint
{
    public DateTime Timestamp { get; set; }
    public Dictionary<string, decimal> Values { get; set; } = new();
}

public class DatabaseStatsDto
{
    public int TotalWallets { get; set; }
    public int TotalCredentials { get; set; }
    public int TotalPersons { get; set; }
    public int TotalOrganizations { get; set; }
    public decimal DatabaseSizeMB { get; set; }
    public DateTime? LastBackup { get; set; }
}

public class AuditLogDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public AuditEventType EventType { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class BackupDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class BackupStatusDto
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int PercentComplete { get; set; }
    public string? CurrentOperation { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EstimatedCompletion { get; set; }
}

public class FeatureFlagDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime? EnabledAt { get; set; }
    public string? EnabledBy { get; set; }
}

public class ConfigurationDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public bool IsSecret { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ReportDto
{
    public string Id { get; set; } = string.Empty;
    public ReportType Type { get; set; }
    public string Format { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public DateTime GeneratedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ScheduledReportDto
{
    public string Id { get; set; } = string.Empty;
    public ReportType Type { get; set; }
    public string Schedule { get; set; } = string.Empty;
    public List<string> Recipients { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime? LastRun { get; set; }
    public DateTime? NextRun { get; set; }
}

// ReportType moved to AdminMutation.cs to avoid duplication

public enum AuditEventType
{
    Create,
    Update,
    Delete,
    Access,
    Login,
    Logout,
    PasswordChange,
    PermissionChange
}

public enum TenantStatus
{
    Active,
    Suspended,
    Pending,
    Deleted
}

// ReportParameters moved to AdminMutation.cs to avoid duplication

// Entity for backup metadata
public class BackupMetadata
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}