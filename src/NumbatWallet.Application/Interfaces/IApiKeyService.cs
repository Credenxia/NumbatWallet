namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for managing API keys and their associated public keys
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Gets the public key associated with an API key
    /// </summary>
    Task<string?> GetPublicKeyAsync(string apiKey);

    /// <summary>
    /// Validates an API key
    /// </summary>
    Task<bool> ValidateApiKeyAsync(string apiKey);

    /// <summary>
    /// Gets tenant ID associated with an API key
    /// </summary>
    Task<Guid?> GetTenantIdAsync(string apiKey);

    /// <summary>
    /// Registers a new API key with its public key
    /// </summary>
    Task<bool> RegisterApiKeyAsync(string apiKey, string publicKey, Guid tenantId);

    /// <summary>
    /// Revokes an API key
    /// </summary>
    Task<bool> RevokeApiKeyAsync(string apiKey);

    /// <summary>
    /// Gets API key metadata
    /// </summary>
    Task<ApiKeyMetadata?> GetApiKeyMetadataAsync(string apiKey);
}

public record ApiKeyMetadata
{
    public string ApiKey { get; init; } = string.Empty;
    public string PublicKey { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public bool IsActive { get; init; }
    public string? Algorithm { get; init; }
}
