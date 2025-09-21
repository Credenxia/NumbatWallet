namespace NumbatWallet.Web.Admin.Models;

/// <summary>
/// Information about a tenant in the system
/// </summary>
public class TenantInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public string Plan { get; set; } = "Standard";
    public int MonthlyUsage { get; set; }
    public int WalletCount { get; set; }
    public int ActiveWalletCount { get; set; }
    public TenantSettings? Settings { get; set; }
}

/// <summary>
/// Configuration settings for a tenant
/// </summary>
public class TenantSettings
{
    public string PrimaryColor { get; set; } = "#4F46E5";
    public string SecondaryColor { get; set; } = "#10B981";
    public int MaxWallets { get; set; } = 10000;
    public int MaxVerificationsPerMonth { get; set; } = 100000;
    public bool EnableWhiteLabeling { get; set; }
    public string? CustomDomain { get; set; }
}

/// <summary>
/// Interface for tenant service operations
/// </summary>
public interface ITenantService
{
    Task<TenantInfo?> GetCurrentTenantAsync();
    Task<TenantInfo?> GetTenantByIdAsync(string tenantId);
    Task<List<TenantInfo>> GetAllTenantsAsync();
    Task<bool> UpdateTenantSettingsAsync(string tenantId, TenantSettings settings);
}