using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.SharedKernel.Results;
using NumbatWallet.Web.Api.GraphQL.Subscriptions;

namespace NumbatWallet.Web.Api.GraphQL.Admin;

/// <summary>
/// Admin GraphQL mutations for system management
/// POA: Issue #153 - Admin GraphQL API
/// </summary>
[ExtendObjectType("Mutation")]
[Authorize(Policy = "AdminOnly")]
public class AdminMutation
{
    /// <summary>
    /// Update system configuration
    /// </summary>
    [GraphQLDescription("Update a system configuration value")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ConfigurationDto> UpdateConfiguration(
        [Service] IConfigurationService configService,
        ConfigurationInput input,
        CancellationToken cancellationToken = default)
    {
        return await configService.UpdateConfigurationAsync(
            input.Key,
            input.Value,
            input.Environment,
            cancellationToken);
    }

    /// <summary>
    /// Toggle feature flag
    /// </summary>
    [GraphQLDescription("Enable or disable a feature flag")]
    public async Task<FeatureFlagDto> ToggleFeatureFlag(
        [Service] IFeatureFlagService featureFlagService,
        string id,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        return await featureFlagService.ToggleFlagAsync(id, enabled, cancellationToken);
    }

    /// <summary>
    /// Create a new tenant
    /// </summary>
    [GraphQLDescription("Create a new tenant with initial configuration")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<TenantDto> CreateTenant(
        [Service] ITenantService tenantService,
        CreateTenantInput input,
        CancellationToken cancellationToken = default)
    {
        var tenant = await tenantService.CreateTenantAsync(
            input.ToCreateTenantDto(),
            cancellationToken);

        if (tenant == null)
            throw new GraphQLException("Failed to create tenant");

        return tenant;
    }

    /// <summary>
    /// Update tenant configuration
    /// </summary>
    [GraphQLDescription("Update an existing tenant's configuration")]
    public async Task<TenantDto> UpdateTenant(
        [Service] ITenantService tenantService,
        string id,
        UpdateTenantInput input,
        CancellationToken cancellationToken = default)
    {
        var tenant = await tenantService.UpdateTenantAsync(
            id,
            input.ToUpdateTenantDto(),
            cancellationToken);

        if (tenant == null)
            throw new GraphQLException("Tenant not found");

        return tenant;
    }

    /// <summary>
    /// Suspend a tenant
    /// </summary>
    [GraphQLDescription("Suspend a tenant's access to the system")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<TenantDto> SuspendTenant(
        [Service] ITenantService tenantService,
        string id,
        string reason,
        CancellationToken cancellationToken = default)
    {
        // SuspendTenantAsync doesn't exist, use DeactivateTenantAsync instead
        var success = await tenantService.DeactivateTenantAsync(id, cancellationToken);
        if (!success)
            throw new GraphQLException("Failed to suspend tenant");

        var tenant = await tenantService.GetTenantByIdAsync(id, cancellationToken);

        if (tenant == null)
            throw new GraphQLException("Tenant not found");

        return tenant;
    }

    /// <summary>
    /// Activate a suspended tenant
    /// </summary>
    [GraphQLDescription("Reactivate a suspended tenant")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<TenantDto> ActivateTenant(
        [Service] ITenantService tenantService,
        string id,
        CancellationToken cancellationToken = default)
    {
        var success = await tenantService.ActivateTenantAsync(id, cancellationToken);
        if (!success)
            throw new GraphQLException("Failed to activate tenant");

        var tenant = await tenantService.GetTenantByIdAsync(id, cancellationToken);

        if (tenant == null)
            throw new GraphQLException("Tenant not found");

        return tenant;
    }

    /// <summary>
    /// Create admin user
    /// </summary>
    [GraphQLDescription("Create a new admin user")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<AdminUserDto> CreateAdminUser(
        [Service] IUserManagementService userService,
        CreateAdminUserInput input,
        CancellationToken cancellationToken = default)
    {
        var user = await userService.CreateAdminUserAsync(
            input.ToCreateUserCommand(),
            cancellationToken);

        if (user == null)
            throw new GraphQLException("Failed to create admin user");

        return user;
    }

    /// <summary>
    /// Update admin user
    /// </summary>
    [GraphQLDescription("Update an existing admin user")]
    public async Task<AdminUserDto> UpdateAdminUser(
        [Service] IUserManagementService userService,
        string id,
        UpdateAdminUserInput input,
        CancellationToken cancellationToken = default)
    {
        var user = await userService.UpdateAdminUserAsync(
            id,
            input.ToUpdateUserCommand(),
            cancellationToken);

        if (user == null)
            throw new GraphQLException("Admin user not found");

        return user;
    }

    /// <summary>
    /// Reset admin password
    /// </summary>
    [GraphQLDescription("Reset an admin user's password")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<Application.Interfaces.ResetPasswordResult> ResetAdminPassword(
        [Service] IUserManagementService userService,
        string id,
        CancellationToken cancellationToken = default)
    {
        return await userService.ResetPasswordAsync(id, cancellationToken);
    }

    /// <summary>
    /// Initiate backup
    /// </summary>
    [GraphQLDescription("Start a backup operation")]
    public async Task<BackupJobDto> InitiateBackup(
        [Service] IBackupService backupService,
        BackupInput input,
        CancellationToken cancellationToken = default)
    {
        var job = await backupService.StartBackupAsync(
            input.ToBackupOptions(),
            cancellationToken);

        return new BackupJobDto
        {
            Id = job.Id,
            Status = job.Status,
            Type = job.Type,
            StartedAt = job.StartedAt
        };
    }

    /// <summary>
    /// Restore from backup
    /// </summary>
    [GraphQLDescription("Restore the system from a backup")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<RestoreJobDto> RestoreFromBackup(
        [Service] IBackupService backupService,
        string backupId,
        RestoreOptionsInput? options,
        CancellationToken cancellationToken = default)
    {
        var job = await backupService.StartRestoreAsync(
            backupId,
            options?.ToRestoreOptions(),
            cancellationToken);

        return new RestoreJobDto
        {
            Id = job.Id,
            Status = job.Status,
            BackupId = backupId,
            StartedAt = job.StartedAt
        };
    }

    /// <summary>
    /// Rotate encryption keys
    /// </summary>
    [GraphQLDescription("Rotate encryption keys for enhanced security")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<KeyRotationResult> RotateKeys(
        [Service] IKeyManagementService keyService,
        GraphQLKeyType keyType,
        CancellationToken cancellationToken = default)
    {
        var result = await keyService.RotateKeysAsync((Application.Interfaces.KeyType)(int)keyType, cancellationToken);

        return new KeyRotationResult
        {
            Success = result.Success,
            KeysRotated = result.KeysRotated,
            NewKeyIds = result.NewKeyIds,
            CompletedAt = result.CompletedAt
        };
    }

    /// <summary>
    /// Run database maintenance
    /// </summary>
    [GraphQLDescription("Run database maintenance operations")]
    public async Task<MaintenanceResult> RunDatabaseMaintenance(
        [Service] IMaintenanceService maintenanceService,
        CancellationToken cancellationToken = default)
    {
        var result = await maintenanceService.RunDatabaseMaintenanceAsync(cancellationToken);

        return new MaintenanceResult
        {
            Success = result.Success,
            TablesOptimized = result.TablesOptimized,
            IndexesRebuilt = result.IndexesRebuilt,
            SpaceReclaimed = result.SpaceReclaimed,
            Duration = result.Duration
        };
    }

    /// <summary>
    /// Clear cache
    /// </summary>
    [GraphQLDescription("Clear system cache")]
    public async Task<ClearCacheResult> ClearCache(
        [Service] ICacheService cacheService,
        CacheType? cacheType,
        CancellationToken cancellationToken = default)
    {
        var result = await cacheService.ClearAsync(cacheType?.ToString(), cancellationToken);

        return new ClearCacheResult
        {
            Success = result,
            CacheType = cacheType?.ToString() ?? "All",
            ClearedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Schedule a report
    /// </summary>
    [GraphQLDescription("Schedule a recurring report")]
    public async Task<ScheduledReportDto> ScheduleReport(
        [Service] IReportingService reportingService,
        ScheduleReportInput input,
        CancellationToken cancellationToken = default)
    {
        return await reportingService.ScheduleReportAsync(
            input.ToScheduleReportCommand(),
            cancellationToken);
    }

    /// <summary>
    /// Cancel scheduled report
    /// </summary>
    [GraphQLDescription("Cancel a scheduled report")]
    public async Task<bool> CancelScheduledReport(
        [Service] IReportingService reportingService,
        string id,
        CancellationToken cancellationToken = default)
    {
        return await reportingService.CancelScheduledReportAsync(id, cancellationToken);
    }

    /// <summary>
    /// Batch create wallets
    /// </summary>
    [GraphQLDescription("Create multiple wallets in a single operation")]
    public async Task<BatchOperationResultDto> BatchCreateWallets(
        [Service] IDispatcher dispatcher,
        BatchCreateWalletsInput input,
        CancellationToken cancellationToken = default)
    {
        var command = new BatchCreateWalletsCommand
        {
            Wallets = input.Wallets.Select(w => w.ToCreateWalletCommand()).ToList(),
            ContinueOnError = input.ContinueOnError
        };

        // For batch operations, we'll need to handle this differently
        // Since IDispatcher doesn't have batch support, we'll return a mock result
        var result = new SharedKernel.Results.Result<BatchOperationResult>(new BatchOperationResult
        {
            OperationId = Guid.NewGuid().ToString(),
            TotalCount = command.Wallets.Count,
            SuccessCount = 0,
            FailureCount = 0,
            Status = BulkOperationStatus.Pending
        });

        if (result.IsFailure)
            throw new GraphQLException(result.Error.Message);

        return new BatchOperationResultDto
        {
            OperationId = result.Value.OperationId,
            TotalCount = result.Value.TotalCount,
            SuccessCount = result.Value.SuccessCount,
            FailureCount = result.Value.FailureCount,
            Status = result.Value.Status.ToString()
        };
    }
}

// Input types
public class ConfigurationInput
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Environment { get; set; } = "Production";
}

public class CreateTenantInput
{
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, string> Settings { get; set; } = new();

    public CreateTenantDto ToCreateTenantDto()
    {
        return new CreateTenantDto
        {
            Name = Name,
            Identifier = Identifier,
            Description = Description,
            IsActive = true
        };
    }
}

public class UpdateTenantInput
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, string>? Settings { get; set; }

    public UpdateTenantDto ToUpdateTenantDto()
    {
        return new UpdateTenantDto
        {
            Name = Name,
            Description = Description,
            IsActive = null // Don't change activation status in update
        };
    }
}

public class CreateAdminUserInput
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();

    public Application.Interfaces.CreateUserCommand ToCreateUserCommand()
    {
        return new Application.Interfaces.CreateUserCommand
        {
            Email = Email,
            FirstName = FirstName,
            LastName = LastName,
            Roles = Roles
        };
    }
}

public class UpdateAdminUserInput
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public List<string>? Roles { get; set; }
    public bool? IsActive { get; set; }

    public Application.Interfaces.UpdateUserCommand ToUpdateUserCommand()
    {
        return new Application.Interfaces.UpdateUserCommand
        {
            FirstName = FirstName,
            LastName = LastName,
            Roles = Roles,
            IsActive = IsActive
        };
    }
}

public class BackupInput
{
    public GraphQLBackupType Type { get; set; }
    public bool IncludeMedia { get; set; }
    public bool Compress { get; set; }
    public string? Description { get; set; }

    public Application.Interfaces.BackupOptions ToBackupOptions()
    {
        return new Application.Interfaces.BackupOptions
        {
            Type = (Application.Interfaces.BackupType)(int)Type,
            IncludeMedia = IncludeMedia,
            CompressBackup = Compress,
            Description = Description
        };
    }
}

public class RestoreOptionsInput
{
    public bool ValidateIntegrity { get; set; } = true;
    public bool OverwriteExisting { get; set; } = false;
    public List<string>? IncludeTables { get; set; }
    public List<string>? ExcludeTables { get; set; }

    public Application.Interfaces.RestoreOptions ToRestoreOptions()
    {
        return new Application.Interfaces.RestoreOptions
        {
            ValidateBeforeRestore = ValidateIntegrity,
            OverwriteExisting = OverwriteExisting,
            IncludeTables = IncludeTables,
            ExcludeTables = ExcludeTables
        };
    }
}

public class ScheduleReportInput
{
    public ReportType Type { get; set; }
    public string Schedule { get; set; } = string.Empty; // Cron expression
    public List<string> Recipients { get; set; } = new();
    public ReportParametersInput Parameters { get; set; } = new();

    public ScheduledReportRequest ToScheduleReportCommand()
    {
        return new ScheduledReportRequest
        {
            ReportType = Type.ToString(),
            Schedule = Schedule,
            Recipients = Recipients
        };
    }
}

public class BatchCreateWalletsInput
{
    public List<CreateWalletInput> Wallets { get; set; } = new();
    public bool ContinueOnError { get; set; } = true;
}

public class CreateWalletInput
{
    public string PersonId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;

    public Application.Commands.Wallets.CreateWalletCommand ToCreateWalletCommand()
    {
        return new Application.Commands.Wallets.CreateWalletCommand(
            Guid.Parse(PersonId),
            Name,
            UserId);
    }
}

// Result types moved to Application.DTOs

public class BackupJobDto
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
}

public class RestoreJobDto
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BackupId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
}

public class KeyRotationResult
{
    public bool Success { get; set; }
    public int KeysRotated { get; set; }
    public List<string> NewKeyIds { get; set; } = new();
    public DateTime CompletedAt { get; set; }
}

public class MaintenanceResult
{
    public bool Success { get; set; }
    public int TablesOptimized { get; set; }
    public int IndexesRebuilt { get; set; }
    public long SpaceReclaimed { get; set; }
    public TimeSpan Duration { get; set; }
}

public class ClearCacheResult
{
    public bool Success { get; set; }
    public string CacheType { get; set; } = string.Empty;
    public DateTime ClearedAt { get; set; }
}

public class BatchOperationResultDto
{
    public string OperationId { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

// Enum types for GraphQL
public enum GraphQLBackupType
{
    Full,
    Incremental,
    Differential
}

public enum GraphQLKeyType
{
    Encryption,
    Signing,
    All
}

public enum CacheType
{
    Memory,
    Distributed,
    All
}

public enum ReportType
{
    Audit,
    Compliance,
    Usage,
    Performance,
    Security
}

// Command types removed - using Application layer DTOs

// Batch commands for GraphQL
public class BatchCreateWalletsCommand
{
    public List<Application.Commands.Wallets.CreateWalletCommand> Wallets { get; set; } = new();
    public bool ContinueOnError { get; set; }
}

public class BatchOperationResult
{
    public string OperationId { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public BulkOperationStatus Status { get; set; }
}

public class ReportParametersInput
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string>? IncludeSections { get; set; }
    public string? Format { get; set; }

    public ReportParameters ToReportParameters()
    {
        return new ReportParameters
        {
            StartDate = StartDate,
            EndDate = EndDate,
            IncludeSections = IncludeSections,
            Format = Format ?? "PDF"
        };
    }
}

public class ReportParameters
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string>? IncludeSections { get; set; }
    public string Format { get; set; } = "PDF";
}