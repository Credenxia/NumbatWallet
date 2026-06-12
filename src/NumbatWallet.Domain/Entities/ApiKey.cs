using System.Security.Cryptography;
using System.Text;
using NumbatWallet.SharedKernel.Primitives;

namespace NumbatWallet.Domain.Entities;

/// <summary>
/// API key entity for programmatic access to the NumbatWallet API.
/// Keys are hashed (SHA256) before storage; the raw key is only available at creation time.
/// </summary>
public class ApiKey : Entity<Guid>
{
    private const string KeyPrefix = "nw_sk_";
    private const int RawKeyLength = 32;

    private ApiKey() : base(Guid.Empty) { } // For EF Core

    private ApiKey(
        Guid id,
        string name,
        string keyHash,
        string prefix,
        List<string> permissions,
        DateTime? expiresAt,
        Guid createdByUserId) : base(id)
    {
        Name = name;
        KeyHash = keyHash;
        Prefix = prefix;
        Permissions = permissions;
        IsActive = true;
        ExpiresAt = expiresAt;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Friendly name for identifying this API key
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// SHA256 hash of the raw API key. Used for validation.
    /// </summary>
    public string KeyHash { get; private set; } = string.Empty;

    /// <summary>
    /// First 8 characters of the raw key for display purposes (e.g., "nw_sk_a1b2...")
    /// </summary>
    public string Prefix { get; private set; } = string.Empty;

    /// <summary>
    /// Permissions granted to this API key
    /// </summary>
    public List<string> Permissions { get; private set; } = [];

    /// <summary>
    /// Whether this key is currently active
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Optional expiration date. Null means the key does not expire.
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// Timestamp of the last time this key was used for authentication
    /// </summary>
    public DateTime? LastUsedAt { get; private set; }

    /// <summary>
    /// The user who created this API key
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// When this API key was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Creates a new API key and returns both the entity and the raw key.
    /// The raw key is only available at creation time and cannot be retrieved later.
    /// </summary>
    /// <param name="name">Friendly name for the key</param>
    /// <param name="permissions">Permissions to grant</param>
    /// <param name="createdByUserId">User creating the key</param>
    /// <param name="expiresAt">Optional expiration date</param>
    /// <returns>Tuple of (ApiKey entity, raw key string)</returns>
    public static (ApiKey ApiKey, string RawKey) Create(
        string name,
        List<string> permissions,
        Guid createdByUserId,
        DateTime? expiresAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(permissions);

        // Generate cryptographically secure random bytes for the key
        var randomBytes = new byte[RawKeyLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var rawKeySuffix = Convert.ToHexString(randomBytes).ToLowerInvariant();
        var rawKey = $"{KeyPrefix}{rawKeySuffix}";

        // Hash the raw key for storage
        var keyHash = ComputeHash(rawKey);

        // Store a displayable prefix (e.g., "nw_sk_a1b2c3d4...")
        var prefix = rawKey.Length > 16
            ? $"{rawKey[..16]}..."
            : rawKey;

        var apiKey = new ApiKey(
            Guid.NewGuid(),
            name,
            keyHash,
            prefix,
            permissions,
            expiresAt,
            createdByUserId);

        return (apiKey, rawKey);
    }

    /// <summary>
    /// Validates a raw key against this API key's hash using constant-time comparison.
    /// Returns true if the key matches and the key is active and not expired.
    /// </summary>
    public bool ValidateKey(string rawKey)
    {
        if (string.IsNullOrEmpty(rawKey))
        {
            return false;
        }

        if (!IsActive)
        {
            return false;
        }

        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow)
        {
            return false;
        }

        var candidateHash = ComputeHash(rawKey);

        var candidateBytes = Encoding.UTF8.GetBytes(candidateHash);
        var storedBytes = Encoding.UTF8.GetBytes(KeyHash);

        // Constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(candidateBytes, storedBytes);
    }

    /// <summary>
    /// Records that this key was used for an API request
    /// </summary>
    public void RecordUsage()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates this API key
    /// </summary>
    public void Revoke()
    {
        IsActive = false;
    }

    /// <summary>
    /// Reactivates a previously revoked API key
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Updates the permissions for this API key
    /// </summary>
    public void UpdatePermissions(List<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        Permissions = permissions;
    }

    /// <summary>
    /// Updates the expiration date
    /// </summary>
    public void UpdateExpiration(DateTime? expiresAt)
    {
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Whether this key has expired
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    private static string ComputeHash(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
