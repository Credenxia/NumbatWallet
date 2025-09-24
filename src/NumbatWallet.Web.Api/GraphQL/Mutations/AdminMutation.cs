using HotChocolate;
using HotChocolate.Types;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Web.Api.Security;
using System.Security.Claims;

namespace NumbatWallet.Web.Api.GraphQL.Mutations;

/// <summary>
/// GraphQL mutations for administrative operations
/// </summary>
[ExtendObjectType("Mutation")]
public class AdminMutation
{
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<AdminMutation> _logger;
    private readonly ISystemMetricsService _metricsService;
    private readonly ICacheService _cacheService;

    public AdminMutation(
        ISecurityAuditService auditService,
        ILogger<AdminMutation> logger,
        ISystemMetricsService metricsService,
        ICacheService cacheService)
    {
        _auditService = auditService;
        _logger = logger;
        _metricsService = metricsService;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Create a new tenant
    /// </summary>
    [GraphQLDescription("Create a new tenant organization")]
    [HotChocolate.Authorization.Authorize(Roles = new[] { "SuperAdmin" })]
    public async Task<TenantDto> CreateTenant(
        CreateTenantInput input,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Creating tenant {TenantName}", input.Name);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.ConfigurationChange,
                $"Tenant created: {input.Name}");
        }

        var tenant = new TenantDto
        {
            Id = Guid.NewGuid(),
            Name = input.Name,
            DisplayName = input.DisplayName,
            Domain = input.Domain,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Settings = input.Settings ?? new Dictionary<string, object>()
        };

        await _cacheService.SetAsync($"tenant:{tenant.Id}", tenant, TimeSpan.FromDays(1));

        return tenant;
    }

    /// <summary>
    /// Update system configuration
    /// </summary>
    [GraphQLDescription("Update system-wide configuration settings")]
    [HotChocolate.Authorization.Authorize(Roles = new[] { "Admin", "SuperAdmin" })]
    public async Task<SystemConfigurationDto> UpdateSystemConfiguration(
        UpdateSystemConfigurationInput input,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogWarning("System configuration update by {UserId}", userId);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.ConfigurationChange,
                $"System configuration updated: {input.ConfigKey}");
        }

        var config = new SystemConfigurationDto
        {
            Key = input.ConfigKey,
            Value = input.ConfigValue,
            Category = input.Category ?? "General",
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId ?? "system"
        };

        await _cacheService.SetAsync($"config:{config.Key}", config, TimeSpan.FromHours(24));

        return config;
    }

    /// <summary>
    /// Trigger system backup
    /// </summary>
    [GraphQLDescription("Initiate a system-wide backup")]
    [HotChocolate.Authorization.Authorize(Roles = new[] { "Admin", "SuperAdmin" })]
    public async Task<BackupResultDto> TriggerBackup(
        BackupInput input,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("System backup initiated by {UserId}", userId);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataAccess,
                $"System backup initiated: {input.BackupType}");
        }

        // TODO: Implement actual backup logic
        var result = new BackupResultDto
        {
            BackupId = Guid.NewGuid(),
            BackupType = input.BackupType ?? "Full",
            Status = "InProgress",
            StartedAt = DateTime.UtcNow,
            InitiatedBy = userId ?? "system",
            EstimatedSizeGB = 10.5,
            BackupLocation = "/backups/" + Guid.NewGuid()
        };

        return result;
    }

    /// <summary>
    /// Clear system cache
    /// </summary>
    [GraphQLDescription("Clear system cache by pattern")]
    [HotChocolate.Authorization.Authorize(Roles = new[] { "Admin", "SuperAdmin" })]
    public async Task<CacheClearResultDto> ClearCache(
        ClearCacheInput input,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogWarning("Cache clear requested by {UserId} for pattern {Pattern}",
            userId, input.Pattern);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.ConfigurationChange,
                $"Cache cleared: {input.Pattern}");
        }

        // TODO: Implement actual cache clearing logic
        var itemsCleared = 0;

        if (input.Pattern == "*" || input.Pattern == "all")
        {
            itemsCleared = 100; // Mock number
        }
        else
        {
            itemsCleared = 10; // Mock number
        }

        var result = new CacheClearResultDto
        {
            Success = true,
            ItemsCleared = itemsCleared,
            Pattern = input.Pattern,
            ClearedAt = DateTime.UtcNow,
            ClearedBy = userId ?? "system"
        };

        return result;
    }

    /// <summary>
    /// Update rate limits
    /// </summary>
    [GraphQLDescription("Update API rate limiting configuration")]
    [HotChocolate.Authorization.Authorize(Roles = new[] { "Admin", "SuperAdmin" })]
    public async Task<RateLimitConfigurationDto> UpdateRateLimits(
        UpdateRateLimitsInput input,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Rate limits updated for policy {PolicyName}", input.PolicyName);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.ConfigurationChange,
                $"Rate limits updated: {input.PolicyName}");
        }

        var config = new RateLimitConfigurationDto
        {
            PolicyName = input.PolicyName,
            RequestsPerMinute = input.RequestsPerMinute,
            RequestsPerHour = input.RequestsPerHour ?? input.RequestsPerMinute * 60,
            BurstSize = input.BurstSize ?? input.RequestsPerMinute * 2,
            IsEnabled = input.IsEnabled ?? true,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId ?? "system"
        };

        await _cacheService.SetAsync($"ratelimit:{config.PolicyName}", config, TimeSpan.FromDays(1));

        return config;
    }

    /// <summary>
    /// Rotate encryption keys
    /// </summary>
    [GraphQLDescription("Rotate system encryption keys")]
    [HotChocolate.Authorization.Authorize(Roles = new[] { "SuperAdmin" })]
    public async Task<KeyRotationResultDto> RotateKeys(
        KeyRotationInput input,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogCritical("Key rotation initiated by {UserId} for key type {KeyType}",
            userId, input.KeyType);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.ConfigurationChange,
                $"Key rotation initiated: {input.KeyType}");
        }

        // TODO: Implement actual key rotation logic
        var result = new KeyRotationResultDto
        {
            Success = true,
            KeyType = input.KeyType,
            OldKeyId = Guid.NewGuid().ToString(),
            NewKeyId = Guid.NewGuid().ToString(),
            RotatedAt = DateTime.UtcNow,
            RotatedBy = userId ?? "system",
            AffectedRecords = 1000
        };

        return result;
    }
}

// Input types for admin mutations
public class CreateTenantInput
{
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public required string Domain { get; set; }
    public Dictionary<string, object>? Settings { get; set; }
}

public class UpdateSystemConfigurationInput
{
    public required string ConfigKey { get; set; }
    public required object ConfigValue { get; set; }
    public string? Category { get; set; }
}

public class BackupInput
{
    public string? BackupType { get; set; }
    public bool? IncludeCredentials { get; set; }
    public bool? IncludeAuditLogs { get; set; }
}

public class ClearCacheInput
{
    public required string Pattern { get; set; }
}

public class UpdateRateLimitsInput
{
    public required string PolicyName { get; set; }
    public required int RequestsPerMinute { get; set; }
    public int? RequestsPerHour { get; set; }
    public int? BurstSize { get; set; }
    public bool? IsEnabled { get; set; }
}

public class KeyRotationInput
{
    public required string KeyType { get; set; }
    public bool? RotateImmediately { get; set; }
}

// DTOs for admin operations
public class TenantDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public required string Domain { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
}

public class SystemConfigurationDto
{
    public required string Key { get; set; }
    public required object Value { get; set; }
    public required string Category { get; set; }
    public DateTime UpdatedAt { get; set; }
    public required string UpdatedBy { get; set; }
}

public class BackupResultDto
{
    public Guid BackupId { get; set; }
    public required string BackupType { get; set; }
    public required string Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public required string InitiatedBy { get; set; }
    public double EstimatedSizeGB { get; set; }
    public required string BackupLocation { get; set; }
}

public class CacheClearResultDto
{
    public bool Success { get; set; }
    public int ItemsCleared { get; set; }
    public required string Pattern { get; set; }
    public DateTime ClearedAt { get; set; }
    public required string ClearedBy { get; set; }
}

public class RateLimitConfigurationDto
{
    public required string PolicyName { get; set; }
    public int RequestsPerMinute { get; set; }
    public int RequestsPerHour { get; set; }
    public int BurstSize { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime UpdatedAt { get; set; }
    public required string UpdatedBy { get; set; }
}

public class KeyRotationResultDto
{
    public bool Success { get; set; }
    public required string KeyType { get; set; }
    public required string OldKeyId { get; set; }
    public required string NewKeyId { get; set; }
    public DateTime RotatedAt { get; set; }
    public required string RotatedBy { get; set; }
    public int AffectedRecords { get; set; }
}