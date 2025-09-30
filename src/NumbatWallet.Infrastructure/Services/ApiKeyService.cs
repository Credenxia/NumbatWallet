using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

/// <summary>
/// Service for managing API keys with Redis caching
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<ApiKeyService> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);

    public ApiKeyService(
        IDistributedCache cache,
        ILogger<ApiKeyService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetPublicKeyAsync(string apiKey)
    {
        var metadata = await GetApiKeyMetadataAsync(apiKey);
        return metadata?.PublicKey;
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        var metadata = await GetApiKeyMetadataAsync(apiKey);
        return metadata?.IsActive == true;
    }

    public async Task<Guid?> GetTenantIdAsync(string apiKey)
    {
        var metadata = await GetApiKeyMetadataAsync(apiKey);
        return metadata?.TenantId;
    }

    public async Task<bool> RegisterApiKeyAsync(string apiKey, string publicKey, Guid tenantId)
    {
        try
        {
            // Create metadata
            var metadata = new ApiKeyMetadata
            {
                ApiKey = HashApiKey(apiKey),
                PublicKey = publicKey,
                TenantId = tenantId,
                CreatedAt = DateTimeOffset.UtcNow,
                IsActive = true,
                Algorithm = "RSA-SHA256"
            };

            // Store in cache
            await StoreApiKeyMetadataAsync(apiKey, metadata);

            _logger.LogInformation("API key registered for tenant {TenantId}", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register API key");
            return false;
        }
    }

    public async Task<bool> RevokeApiKeyAsync(string apiKey)
    {
        try
        {
            var metadata = await GetApiKeyMetadataAsync(apiKey);
            if (metadata == null)
            {
                return false;
            }

            metadata = metadata with { IsActive = false };
            await StoreApiKeyMetadataAsync(apiKey, metadata);

            _logger.LogInformation("API key revoked for tenant {TenantId}", metadata.TenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke API key");
            return false;
        }
    }

    public async Task<ApiKeyMetadata?> GetApiKeyMetadataAsync(string apiKey)
    {
        try
        {
            var key = GetCacheKey(apiKey);
            var json = await _cache.GetStringAsync(key);

            if (string.IsNullOrEmpty(json))
            {
                // Try to load from database (future enhancement)
                return null;
            }

            var metadata = System.Text.Json.JsonSerializer.Deserialize<ApiKeyMetadata>(json);

            // Update last used timestamp
            if (metadata != null && metadata.IsActive)
            {
                metadata = metadata with { LastUsedAt = DateTimeOffset.UtcNow };
                await StoreApiKeyMetadataAsync(apiKey, metadata);
            }

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get API key metadata");
            return null;
        }
    }

    private async Task StoreApiKeyMetadataAsync(string apiKey, ApiKeyMetadata metadata)
    {
        var key = GetCacheKey(apiKey);
        var json = System.Text.Json.JsonSerializer.Serialize(metadata);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration,
            SlidingExpiration = TimeSpan.FromMinutes(15)
        };

        await _cache.SetStringAsync(key, json, options);
    }

    private string GetCacheKey(string apiKey)
    {
        var hashedKey = HashApiKey(apiKey);
        return $"apikey:{hashedKey}";
    }

    private string HashApiKey(string apiKey)
    {
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
