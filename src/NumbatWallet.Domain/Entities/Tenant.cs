using NumbatWallet.SharedKernel.Primitives;

namespace NumbatWallet.Domain.Entities;

/// <summary>
/// Tenant entity for multi-tenancy support
/// </summary>
public class Tenant : Entity<Guid>
{
    /// <summary>
    /// Default constructor for EF Core
    /// </summary>
    public Tenant() : base(Guid.Empty)
    {
    }

    /// <summary>
    /// Constructor with ID
    /// </summary>
    public Tenant(Guid id) : base(id)
    {
    }

    /// <summary>
    /// Tenant display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier for the tenant (used in URLs, etc.)
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Whether the tenant is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Subscription tier (Basic, Professional, Enterprise)
    /// </summary>
    public string SubscriptionTier { get; set; } = "Basic";

    /// <summary>
    /// Tenant-specific settings stored as JSON
    /// </summary>
    public Dictionary<string, object> Settings { get; set; } = new();

    /// <summary>
    /// Date when the tenant was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update date
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// User who created the tenant
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// User who last updated the tenant
    /// </summary>
    public string? UpdatedBy { get; set; }

    // Navigation properties commented out until entities are properly defined
    // public virtual ICollection<Person> Persons { get; set; } = new List<Person>();
    // public virtual ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();
    // public virtual ICollection<Organization> Organizations { get; set; } = new List<Organization>();

    /// <summary>
    /// Check if tenant has feature enabled
    /// </summary>
    public bool HasFeature(string feature)
    {
        if (!Settings.TryGetValue("features", out var featuresObj))
        {
            return false;
        }

        if (featuresObj is List<string> features)
        {
            return features.Contains("all") || features.Contains(feature);
        }

        return false;
    }

    /// <summary>
    /// Get maximum allowed users for this tenant
    /// </summary>
    public int GetMaxUsers()
    {
        if (Settings.TryGetValue("maxUsers", out var maxUsersObj) && maxUsersObj is int maxUsers)
        {
            return maxUsers;
        }
        return 100; // Default
    }

    /// <summary>
    /// Get maximum allowed wallets for this tenant
    /// </summary>
    public int GetMaxWallets()
    {
        if (Settings.TryGetValue("maxWallets", out var maxWalletsObj) && maxWalletsObj is int maxWallets)
        {
            return maxWallets;
        }
        return 100; // Default
    }
}