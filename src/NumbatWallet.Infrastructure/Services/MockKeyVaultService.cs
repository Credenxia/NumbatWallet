using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

public class MockKeyVaultService : IKeyVaultService
{
    private readonly Dictionary<string, string> _secrets = new();
    private readonly ILogger<MockKeyVaultService> _logger;

    public MockKeyVaultService(ILogger<MockKeyVaultService> logger)
    {
        _logger = logger;
        
        // Add some default secrets for development
        _secrets["DatabasePassword"] = "DevPassword123!";
        _secrets["ApiKey"] = "dev-api-key-12345";
        _secrets["JwtSecret"] = "dev-jwt-secret-must-be-at-least-32-characters-long";
        _secrets["EncryptionKey"] = "dev-encryption-key-256-bits-long-for-aes-256bit";
        
        _logger.LogWarning("Using MockKeyVaultService - NOT FOR PRODUCTION USE");
    }

    public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be empty", nameof(secretName));
        }

        _logger.LogDebug("Mock: Getting secret '{SecretName}'", secretName);
        
        if (_secrets.TryGetValue(secretName, out var value))
        {
            return Task.FromResult<string?>(value);
        }
        
        _logger.LogWarning("Mock: Secret '{SecretName}' not found", secretName);
        return Task.FromResult<string?>(null);
    }

    public Task<bool> SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be empty", nameof(secretName));
        }

        _logger.LogDebug("Mock: Setting secret '{SecretName}'", secretName);
        _secrets[secretName] = secretValue;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be empty", nameof(secretName));
        }

        _logger.LogDebug("Mock: Deleting secret '{SecretName}'", secretName);
        return Task.FromResult(_secrets.Remove(secretName));
    }

    public Task<Dictionary<string, string>> GetSecretsAsync(IEnumerable<string> secretNames, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, string>();
        
        foreach (var secretName in secretNames)
        {
            if (_secrets.TryGetValue(secretName, out var value))
            {
                result[secretName] = value;
            }
        }
        
        _logger.LogDebug("Mock: Retrieved {Count} secrets", result.Count);
        return Task.FromResult(result);
    }

    public Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be empty", nameof(secretName));
        }

        var exists = _secrets.ContainsKey(secretName);
        _logger.LogDebug("Mock: Secret '{SecretName}' exists: {Exists}", secretName, exists);
        return Task.FromResult(exists);
    }

    public void ClearCache()
    {
        _logger.LogDebug("Mock: Cache cleared (no-op for mock service)");
        // No-op for mock service as we don't cache separately
    }
}