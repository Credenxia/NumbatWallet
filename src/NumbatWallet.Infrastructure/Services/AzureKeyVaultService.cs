using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

public class AzureKeyVaultService : IKeyVaultService
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<AzureKeyVaultService> _logger;
    private readonly Dictionary<string, string> _cache = new();

    public AzureKeyVaultService(
        IConfiguration configuration,
        ILogger<AzureKeyVaultService> logger)
    {
        _logger = logger;

        var keyVaultUrl = configuration["Azure:KeyVault:Url"]
            ?? throw new InvalidOperationException("Azure Key Vault URL is not configured");

        // Use DefaultAzureCredential which works with:
        // - EnvironmentCredential
        // - ManagedIdentityCredential
        // - SharedTokenCacheCredential
        // - VisualStudioCredential
        // - VisualStudioCodeCredential
        // - AzureCliCredential
        // - AzurePowerShellCredential
        // - InteractiveBrowserCredential
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = false,
            ExcludeManagedIdentityCredential = false,
            ExcludeAzureCliCredential = false,
            ExcludeVisualStudioCredential = false,
            ExcludeVisualStudioCodeCredential = false,
            ExcludeAzurePowerShellCredential = false,
            ExcludeInteractiveBrowserCredential = true // Avoid interactive in production
        });

        _secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        _logger.LogInformation("Azure Key Vault client initialized for: {KeyVaultUrl}", keyVaultUrl);
    }

    public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be empty", nameof(secretName));
        }

        // Check cache first
        if (_cache.TryGetValue(secretName, out var cachedValue))
        {
            _logger.LogDebug("Retrieved secret '{SecretName}' from cache", secretName);
            return cachedValue;
        }

        try
        {
            var response = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            var secretValue = response.Value.Value;

            // Cache the secret value
            _cache[secretName] = secretValue;

            _logger.LogInformation("Retrieved secret '{SecretName}' from Key Vault", secretName);
            return secretValue;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret '{SecretName}' not found in Key Vault", secretName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret '{SecretName}' from Key Vault", secretName);
            throw;
        }
    }

    public async Task<bool> SetSecretAsync(
        string secretName,
        string secretValue,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be empty", nameof(secretName));
        }

        if (string.IsNullOrWhiteSpace(secretValue))
        {
            throw new ArgumentException("Secret value cannot be empty", nameof(secretValue));
        }

        try
        {
            await _secretClient.SetSecretAsync(secretName, secretValue, cancellationToken);

            // Update cache
            _cache[secretName] = secretValue;

            _logger.LogInformation("Set secret '{SecretName}' in Key Vault", secretName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting secret '{SecretName}' in Key Vault", secretName);
            return false;
        }
    }

    public async Task<bool> DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be empty", nameof(secretName));
        }

        try
        {
            var deleteOperation = await _secretClient.StartDeleteSecretAsync(secretName, cancellationToken);

            // Remove from cache
            _cache.Remove(secretName);

            _logger.LogInformation("Started deletion of secret '{SecretName}' from Key Vault", secretName);

            // Note: The actual deletion is asynchronous and may take time
            // The secret will be soft-deleted first and can be recovered
            return true;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret '{SecretName}' not found in Key Vault for deletion", secretName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting secret '{SecretName}' from Key Vault", secretName);
            return false;
        }
    }

    public async Task<Dictionary<string, string>> GetSecretsAsync(
        IEnumerable<string> secretNames,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, string>();

        foreach (var secretName in secretNames)
        {
            var value = await GetSecretAsync(secretName, cancellationToken);
            if (value != null)
            {
                results[secretName] = value;
            }
        }

        return results;
    }

    public void ClearCache()
    {
        _cache.Clear();
        _logger.LogInformation("Cleared Key Vault secret cache");
    }

    public async Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be empty", nameof(secretName));
        }

        try
        {
            await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            return true;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if secret '{SecretName}' exists in Key Vault", secretName);
            throw;
        }
    }
}