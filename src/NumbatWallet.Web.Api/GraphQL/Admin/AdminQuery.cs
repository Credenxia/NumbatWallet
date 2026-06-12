using Microsoft.EntityFrameworkCore;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Infrastructure.Data;

namespace NumbatWallet.Web.Api.GraphQL.Admin;

/// <summary>
/// Admin GraphQL queries for system management
/// POA: Issue #153 - Admin GraphQL API
/// </summary>
[ExtendObjectType("Query")]
public class AdminQuery
{
    /// <summary>
    /// Get system health status
    /// </summary>
    [GraphQLDescription("Get current system health and component status")]
    [Authorize(Policy = "AdminOnly")]
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
    [Authorize(Policy = "AdminOnly")]
    public async Task<MetricsSnapshotDto> GetMetrics(
        [Service] IStatisticsService statisticsService,
        TimeRangeInput timeRange,
        CancellationToken cancellationToken = default)
    {
        var from = DateTime.UtcNow.Add(-timeRange.GetTimeSpan());
        var to = DateTime.UtcNow;

        // Get metrics from the statistics service
        var metrics = await statisticsService.GetMetricsSnapshotAsync(from, to, cancellationToken);

        // Convert Application DTO to Web DTO
        return new MetricsSnapshotDto
        {
            From = metrics.From,
            To = metrics.To,
            Metrics = metrics.Metrics,
            TimeSeries = metrics.TimeSeries.Select(ts => new TimeSeriesDataPoint
            {
                Timestamp = ts.Timestamp,
                Values = new Dictionary<string, decimal> { { ts.Label ?? "value", ts.Value } }
            }).ToList()
        };
    }

    /// <summary>
    /// Get audit logs with filtering
    /// </summary>
    [GraphQLDescription("Query audit logs with filtering and pagination")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    [Authorize(Policy = "AdminOnly")]
    public IQueryable<AuditLogDto> GetAuditLogs(
        [Service] NumbatWalletDbContext context,
        AuditLogFilterInput? filter = null)
    {
        var query = context.AuditLogs.AsQueryable();

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.UserId))
            {
                query = query.Where(a => a.UserId == filter.UserId);
            }

            if (!string.IsNullOrEmpty(filter.Action))
            {
                query = query.Where(a => a.Action.Contains(filter.Action));
            }

            if (filter.From.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= filter.From.Value);
            }

            if (filter.To.HasValue)
            {
                query = query.Where(a => a.CreatedAt <= filter.To.Value);
            }

            if (filter.EventType.HasValue)
            {
                var filterEventType = (SharedKernel.Enums.AuditEventType)filter.EventType.Value;
                query = query.Where(a => a.EventType == filterEventType);
            }
        }

        return query.Select(a => new AuditLogDto
        {
            Id = a.Id.ToString(),
            UserId = a.UserId,
            Action = a.Action,
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            OldValues = a.OldValues,
            NewValues = a.NewValues,
            IpAddress = a.IpAddress,
            UserAgent = a.UserAgent,
            EventType = (AuditEventType)(int)a.EventType, // Convert SharedKernel enum to GraphQL enum
            CreatedAt = a.CreatedAt.DateTime
        });
    }

    /// <summary>
    /// Get all tenants with filtering
    /// </summary>
    [GraphQLDescription("Query all tenants with filtering and pagination")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IQueryable<TenantDto>> GetTenants(
        [Service] ITenantService tenantService,
        TenantFilterInput? filter = null,
        CancellationToken cancellationToken = default)
    {
        var tenants = await tenantService.GetAllTenants(cancellationToken);
        var query = tenants.AsQueryable();

        if (filter != null)
        {
            if (filter.Status.HasValue)
            {
                query = query.Where(t => t.IsActive == (filter.Status.Value == TenantStatus.Active));
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(t =>
                    t.Name.Contains(filter.SearchTerm) ||
                    t.Identifier.Contains(filter.SearchTerm));
            }

            if (filter.CreatedAfter.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= filter.CreatedAfter.Value);
            }
        }

        return query;
    }

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    [GraphQLDescription("Get detailed information about a specific tenant")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<TenantDto?> GetTenant(
        [Service] ITenantService tenantService,
        string id,
        CancellationToken cancellationToken = default)
    {
        return await tenantService.GetTenantByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// Get admin users — READ-ONLY view over the LEGACY admin_users table.
    /// The admin-user mutations (create/update/reset password) were deleted in the
    /// Credentry federation cleanup: user/role management is owned by the Credentry
    /// portal (credentry/docs/integration/06-NUMBATWALLET-FEDERATION-CONTRACT.md).
    /// The table is no longer written to by this API.
    /// </summary>
    [GraphQLDescription("Query admin users (read-only legacy table; user management lives in the Credentry portal)")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    [Authorize(Policy = "AdminOnly")]
    public IQueryable<AdminUserDto> GetAdminUsers(
        [Service] NumbatWalletDbContext context,
        UserFilterInput? filter = null)
    {
        var query = context.AdminUsers.AsQueryable();

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
            {
                query = query.Where(u => u.IsActive == filter.IsActive.Value);
            }

            // Note: Role filtering would require a more complex query since Roles is a collection
            // For now we'll skip it in the database query and can filter in memory if needed
        }

        return query.Select(u => new AdminUserDto
        {
            Id = u.Id.ToString(),
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Roles = new List<string>(), // EF Core doesn't support projecting collections from backing fields easily
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt
        });
    }

    /// <summary>
    /// Get backup history
    /// </summary>
    [GraphQLDescription("Query backup history with pagination")]
    [UsePaging]
    [UseSorting]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IQueryable<BackupDto>> GetBackups(
        [Service] IBackupService backupService,
        CancellationToken cancellationToken = default)
    {
        var backups = await backupService.GetBackupHistoryAsync(cancellationToken);
        // Convert from Application DTOs to Web DTOs
        var webBackups = backups.Select(b => new BackupDto
        {
            Id = b.Id,
            Type = b.Type,
            Status = b.Status,
            SizeBytes = b.SizeBytes,
            Location = b.Location,
            CreatedAt = b.CreatedAt,
            CompletedAt = b.CompletedAt
        }).ToList();
        return webBackups.AsQueryable();
    }

    /// <summary>
    /// Get backup status
    /// </summary>
    [GraphQLDescription("Get the status of a specific backup operation")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<BackupStatusDto?> GetBackupStatus(
        [Service] IBackupService backupService,
        string id,
        CancellationToken cancellationToken = default)
    {
        var status = await backupService.GetBackupStatusAsync(id, cancellationToken);
        if (status == null)
        {
            return null;
        }

        // Convert from Application DTO to Web DTO
        return new BackupStatusDto
        {
            Id = status.Id,
            Status = status.Status,
            PercentComplete = status.PercentComplete,
            CurrentOperation = status.CurrentOperation,
            StartedAt = status.StartedAt,
            EstimatedCompletion = status.EstimatedCompletion
        };
    }

    /// <summary>
    /// Get feature flags
    /// </summary>
    [GraphQLDescription("Get all feature flags and their current states")]
    [Authorize(Policy = "AdminOnly")]
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
    [Authorize(Policy = "AdminOnly")]
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
    [Authorize(Policy = "AdminOnly")]
    public async Task<ReportDto> GenerateReport(
        [Service] IReportingService reportingService,
        ReportType type,
        ReportParametersInput parameters,
        CancellationToken cancellationToken = default)
    {
        // Convert to Application layer types
        var appType = Enum.Parse<Application.Interfaces.ReportType>(type.ToString());
        var appParams = new Application.Interfaces.ReportParameters
        {
            StartDate = parameters.StartDate,
            EndDate = parameters.EndDate,
            IncludeSections = parameters.IncludeSections,
            Format = parameters.Format ?? "PDF"
        };

        var report = await reportingService.GenerateReportAsync(appType, appParams, cancellationToken);

        // Convert back to Web DTO
        return new ReportDto
        {
            Id = report.Id,
            Type = type,
            Format = report.Format,
            Content = report.Content,
            GeneratedAt = report.GeneratedAt,
            Metadata = report.Metadata
        };
    }

    /// <summary>
    /// Get scheduled reports
    /// </summary>
    [GraphQLDescription("Get all scheduled reports")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<List<ScheduledReportDto>> GetScheduledReports(
        [Service] IReportingService reportingService,
        CancellationToken cancellationToken = default)
    {
        var reports = await reportingService.GetScheduledReportsAsync(cancellationToken);

        // Convert from Application DTOs to Web DTOs
        return reports.Select(r => new ScheduledReportDto
        {
            Id = r.Id,
            Type = Enum.Parse<ReportType>(r.Type.ToString()),
            Schedule = r.Schedule,
            Recipients = r.Recipients,
            IsActive = r.IsActive,
            LastRun = r.LastRun,
            NextRun = r.NextRun
        }).ToList();
    }

    /// <summary>
    /// Get wallets for admin management views.
    /// Tenant scoping: the wallet repository goes through the DbContext global query filter,
    /// which is bound to the ambient SharedKernel.ITenantService tenant — admins only ever
    /// see wallets belonging to their own tenant (no cross-tenant leakage).
    /// NOTE: IWalletService is SCOPED and must stay a [Service] resolver parameter
    /// (HotChocolate type extensions are singletons — captive-dependency rule).
    /// </summary>
    [GraphQLDescription("Query wallets in the current tenant for admin management views")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<List<WalletDto>> GetAdminWallets(
        [Service] IWalletService walletService,
        string? search = null,
        int? first = null,
        CancellationToken cancellationToken = default)
    {
        var wallets = await walletService.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(search))
        {
            wallets = wallets.Where(w =>
                w.Id.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                w.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                w.PersonName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var take = first is > 0 ? Math.Min(first.Value, 200) : 100;
        return wallets
            .OrderByDescending(w => w.CreatedAt)
            .Take(take)
            .ToList();
    }

    /// <summary>
    /// Get database statistics
    /// </summary>
    [GraphQLDescription("Get database usage and performance statistics")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<DatabaseStatsDto> GetDatabaseStats(
        [Service] NumbatWalletDbContext context,
        CancellationToken cancellationToken = default)
    {
        var stats = new DatabaseStatsDto
        {
            TotalWallets = await context.Wallets.CountAsync(cancellationToken),
            TotalCredentials = await context.Credentials.CountAsync(cancellationToken),
            TotalPersons = await context.Persons.CountAsync(cancellationToken),
            TotalOrganizations = 0, // Organizations entity not yet implemented
            DatabaseSizeMB = await GetDatabaseSizeAsync(context, cancellationToken),
            // POA: no backup metadata table exists in the EF model yet, so this is honestly null.
            // (Previously queried context.Set<BackupMetadata>() which threw at runtime because
            // BackupMetadata is not a mapped entity.)
            LastBackup = null
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

// SystemHealthDto and ComponentHealthDto are defined in Application.DTOs namespace

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

// AdminUserDto is defined in Application.Interfaces namespace

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

// ConfigurationDto and FeatureFlagDto are defined in Application.DTOs namespace

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
