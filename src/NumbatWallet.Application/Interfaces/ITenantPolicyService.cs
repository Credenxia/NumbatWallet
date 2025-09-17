using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for managing tenant-specific security and retention policies
/// </summary>
public interface ITenantPolicyService
{
    /// <summary>
    /// Gets the current tenant's security policy
    /// </summary>
    /// <returns>The tenant security policy</returns>
    Task<TenantSecurityPolicy> GetCurrentPolicyAsync();

    /// <summary>
    /// Gets a specific tenant's security policy
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>The tenant security policy</returns>
    Task<TenantSecurityPolicy> GetTenantPolicyAsync(Guid tenantId);

    /// <summary>
    /// Gets the field protection rule for a specific field
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="entityType">The entity type</param>
    /// <param name="fieldName">The field name</param>
    /// <returns>The field protection rule</returns>
    Task<FieldProtectionRule> GetFieldPolicyAsync(
        Guid tenantId,
        string entityType,
        string fieldName);

    /// <summary>
    /// Updates a tenant's security policy
    /// </summary>
    /// <param name="policy">The updated policy</param>
    Task UpdatePolicyAsync(TenantSecurityPolicy policy);

    /// <summary>
    /// Checks if a field requires encryption for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="entityType">The entity type</param>
    /// <param name="fieldName">The field name</param>
    /// <returns>True if encryption is required</returns>
    Task<bool> RequiresEncryptionAsync(
        Guid tenantId,
        string entityType,
        string fieldName);

    /// <summary>
    /// Gets the retention policy for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>The retention policy</returns>
    Task<RetentionPolicy> GetRetentionPolicyAsync(Guid tenantId);
}

/// <summary>
/// Tenant-specific security policy configuration
/// </summary>
public class TenantSecurityPolicy
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public List<FieldProtectionRule> FieldRules { get; set; } = new();
    public UnmaskingPolicy UnmaskingPolicy { get; set; } = new();
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets the protection rule for a specific field
    /// </summary>
    public FieldProtectionRule? GetFieldPolicy(string entityType, string fieldName)
    {
        return FieldRules.FirstOrDefault(r =>
            r.EntityType == entityType &&
            r.FieldName == fieldName);
    }
}

/// <summary>
/// Protection rule for a specific field
/// </summary>
public class FieldProtectionRule
{
    public string EntityType { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public DataClassification MinimumClassification { get; set; }
    public bool EnableEncryption { get; set; }
    public bool EnableTokenization { get; set; } = true;
    public RedactionPattern RedactionPattern { get; set; }
    public SearchStrategy SearchStrategy { get; set; }
    public bool RequireReasonForUnmask { get; set; }
    public int? MaxUnmaskDurationSeconds { get; set; }
}

/// <summary>
/// Policy for unmask operations
/// </summary>
public class UnmaskingPolicy
{
    public int DefaultUnmaskDurationSeconds { get; set; } = 300;
    public int MaxUnmaskDurationSeconds { get; set; } = 3600;
    public bool RequireMfaForUnmask { get; set; } = false;
    public DataClassification RequireReasonThreshold { get; set; } = DataClassification.OfficialSensitive;
    public int MaxConcurrentSessions { get; set; } = 10;
    public Dictionary<DataClassification, int> MaxUnmasksByClassification { get; set; } = new();
}

/// <summary>
/// Retention policy configuration
/// </summary>
public class RetentionPolicy
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<RetentionRule> Rules { get; set; } = new();
    public PurgeStrategy DefaultStrategy { get; set; } = PurgeStrategy.Scheduled;
}

/// <summary>
/// Retention rule for specific data classification
/// </summary>
public class RetentionRule
{
    public DataClassification Classification { get; set; }
    public string? EntityType { get; set; }
    public string? FieldName { get; set; }
    public int RetentionDays { get; set; }
    public PurgeAction PurgeAction { get; set; }
    public PurgeStrategy PurgeStrategy { get; set; }
}