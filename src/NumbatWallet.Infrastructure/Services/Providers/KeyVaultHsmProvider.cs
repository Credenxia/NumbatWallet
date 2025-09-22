using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using AzureCrypto = Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Infrastructure.Services.Providers;

/// <summary>
/// Azure Key Vault Premium HSM provider for production Phase 1
/// Provides HSM-backed keys in Key Vault Premium tier
/// </summary>
public class KeyVaultHsmProvider : IHsmProvider
{
    private readonly KeyClient _keyClient;
    private readonly SecretClient _secretClient;
    private readonly ILogger<KeyVaultHsmProvider> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly Dictionary<string, AzureCrypto.CryptographyClient> _cryptoClients;

    public string ProviderType => "KeyVault";
    public bool SupportsHardwareBackedKeys => true; // Premium tier supports HSM-backed keys
    public FipsComplianceLevel ComplianceLevel => FipsComplianceLevel.Level1; // Software-protected in HSM

    public KeyVaultHsmProvider(
        IConfiguration configuration,
        ILogger<KeyVaultHsmProvider> logger,
        IMemoryCache cache)
    {
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
        _cryptoClients = new Dictionary<string, AzureCrypto.CryptographyClient>();

        var keyVaultUri = configuration["KeyVault:Uri"]
            ?? throw new InvalidOperationException("KeyVault:Uri not configured");

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = configuration["KeyVault:ManagedIdentityClientId"],
            ExcludeEnvironmentCredential = false,
            ExcludeAzureCliCredential = false
        });

        _keyClient = new KeyClient(new Uri(keyVaultUri), credential);
        _secretClient = new SecretClient(new Uri(keyVaultUri), credential);

        _logger.LogInformation("Key Vault HSM Provider initialized with URI: {Uri}", keyVaultUri);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test connectivity by listing keys (with limit of 1)
            await _keyClient.GetPropertiesOfKeysAsync(cancellationToken).FirstAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<HsmKey> GenerateKeyAsync(
        KeyGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating {Type} key in Key Vault: {Name}", request.KeyType, request.KeyName);

        var keyOptions = new CreateKeyOptions(request.KeyName, request.KeyType switch
        {
            KeyType.RSA => KeyType.RSA.ToString() == "RSA" ? KeyVaultKeyType.Rsa : KeyVaultKeyType.RsaHsm,
            KeyType.EC => KeyType.EC.ToString() == "EC" ? KeyVaultKeyType.Ec : KeyVaultKeyType.EcHsm,
            _ => throw new NotSupportedException($"Key type {request.KeyType} not supported in Key Vault")
        })
        {
            Enabled = true,
            ExpiresOn = request.ExpiresOn,
            KeySize = request.KeySize,
            HardwareProtected = true // Use HSM-backed keys in Premium tier
        };

        // Add key operations
        if (request.Usage.HasFlag(KeyUsage.Sign))
            keyOptions.KeyOperations.Add(KeyOperation.Sign);
        if (request.Usage.HasFlag(KeyUsage.Verify))
            keyOptions.KeyOperations.Add(KeyOperation.Verify);
        if (request.Usage.HasFlag(KeyUsage.Encrypt))
            keyOptions.KeyOperations.Add(KeyOperation.Encrypt);
        if (request.Usage.HasFlag(KeyUsage.Decrypt))
            keyOptions.KeyOperations.Add(KeyOperation.Decrypt);
        if (request.Usage.HasFlag(KeyUsage.WrapKey))
            keyOptions.KeyOperations.Add(KeyOperation.WrapKey);
        if (request.Usage.HasFlag(KeyUsage.UnwrapKey))
            keyOptions.KeyOperations.Add(KeyOperation.UnwrapKey);

        // Add tags
        foreach (var tag in request.Tags)
        {
            keyOptions.Tags.Add(tag.Key, tag.Value);
        }

        var keyResponse = await _keyClient.CreateKeyAsync(keyOptions, cancellationToken);

        return ConvertToHsmKey(keyResponse.Value);
    }

    public async Task<byte[]> SignAsync(
        string keyId,
        byte[] data,
        SigningAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        var cryptoClient = await GetCryptoClientAsync(keyId, cancellationToken);

        var signAlgorithm = algorithm switch
        {
            SigningAlgorithm.RS256 => SignatureAlgorithm.RS256,
            SigningAlgorithm.RS384 => SignatureAlgorithm.RS384,
            SigningAlgorithm.RS512 => SignatureAlgorithm.RS512,
            SigningAlgorithm.PS256 => SignatureAlgorithm.PS256,
            SigningAlgorithm.PS384 => SignatureAlgorithm.PS384,
            SigningAlgorithm.PS512 => SignatureAlgorithm.PS512,
            SigningAlgorithm.ES256 => SignatureAlgorithm.ES256,
            SigningAlgorithm.ES384 => SignatureAlgorithm.ES384,
            SigningAlgorithm.ES512 => SignatureAlgorithm.ES512,
            _ => throw new NotSupportedException($"Signing algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.SignDataAsync(signAlgorithm, data, cancellationToken);

        _logger.LogDebug("Signed data with key {KeyId} using {Algorithm}", keyId, algorithm);
        return result.Signature;
    }

    public async Task<bool> VerifyAsync(
        string keyId,
        byte[] data,
        byte[] signature,
        SigningAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        var cryptoClient = await GetCryptoClientAsync(keyId, cancellationToken);

        var signAlgorithm = algorithm switch
        {
            SigningAlgorithm.RS256 => SignatureAlgorithm.RS256,
            SigningAlgorithm.RS384 => SignatureAlgorithm.RS384,
            SigningAlgorithm.RS512 => SignatureAlgorithm.RS512,
            SigningAlgorithm.PS256 => SignatureAlgorithm.PS256,
            SigningAlgorithm.PS384 => SignatureAlgorithm.PS384,
            SigningAlgorithm.PS512 => SignatureAlgorithm.PS512,
            SigningAlgorithm.ES256 => SignatureAlgorithm.ES256,
            SigningAlgorithm.ES384 => SignatureAlgorithm.ES384,
            SigningAlgorithm.ES512 => SignatureAlgorithm.ES512,
            _ => throw new NotSupportedException($"Signing algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.VerifyDataAsync(signAlgorithm, data, signature, cancellationToken);
        return result.IsValid;
    }

    public async Task<byte[]> EncryptAsync(
        string keyId,
        byte[] plaintext,
        Domain.Interfaces.EncryptionAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        var cryptoClient = await GetCryptoClientAsync(keyId, cancellationToken);

        var encryptAlgorithm = algorithm switch
        {
            Domain.Interfaces.EncryptionAlgorithm.RSA_OAEP => AzureCrypto.EncryptionAlgorithm.RsaOaep,
            Domain.Interfaces.EncryptionAlgorithm.RSA_OAEP_256 => AzureCrypto.EncryptionAlgorithm.RsaOaep256,
            _ => throw new NotSupportedException($"Encryption algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.EncryptAsync(encryptAlgorithm, plaintext, cancellationToken);
        return result.Ciphertext;
    }

    public async Task<byte[]> DecryptAsync(
        string keyId,
        byte[] ciphertext,
        Domain.Interfaces.EncryptionAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        var cryptoClient = await GetCryptoClientAsync(keyId, cancellationToken);

        var encryptAlgorithm = algorithm switch
        {
            Domain.Interfaces.EncryptionAlgorithm.RSA_OAEP => AzureCrypto.EncryptionAlgorithm.RsaOaep,
            Domain.Interfaces.EncryptionAlgorithm.RSA_OAEP_256 => AzureCrypto.EncryptionAlgorithm.RsaOaep256,
            _ => throw new NotSupportedException($"Encryption algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.DecryptAsync(encryptAlgorithm, ciphertext, cancellationToken);
        return result.Plaintext;
    }

    public async Task<byte[]> WrapKeyAsync(
        string wrappingKeyId,
        byte[] keyToWrap,
        Domain.Interfaces.KeyWrapAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        var cryptoClient = await GetCryptoClientAsync(wrappingKeyId, cancellationToken);

        var wrapAlgorithm = algorithm switch
        {
            Domain.Interfaces.KeyWrapAlgorithm.RSA_OAEP => AzureCrypto.KeyWrapAlgorithm.RsaOaep,
            Domain.Interfaces.KeyWrapAlgorithm.RSA_OAEP_256 => AzureCrypto.KeyWrapAlgorithm.RsaOaep256,
            _ => throw new NotSupportedException($"Key wrap algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.WrapKeyAsync(wrapAlgorithm, keyToWrap, cancellationToken);
        return result.EncryptedKey;
    }

    public async Task<byte[]> UnwrapKeyAsync(
        string unwrappingKeyId,
        byte[] wrappedKey,
        Domain.Interfaces.KeyWrapAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        var cryptoClient = await GetCryptoClientAsync(unwrappingKeyId, cancellationToken);

        var wrapAlgorithm = algorithm switch
        {
            Domain.Interfaces.KeyWrapAlgorithm.RSA_OAEP => AzureCrypto.KeyWrapAlgorithm.RsaOaep,
            Domain.Interfaces.KeyWrapAlgorithm.RSA_OAEP_256 => AzureCrypto.KeyWrapAlgorithm.RsaOaep256,
            _ => throw new NotSupportedException($"Key wrap algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.UnwrapKeyAsync(wrapAlgorithm, wrappedKey, cancellationToken);
        return result.Key;
    }

    public async Task<KeyBackupData> BackupKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Backing up key {KeyId}", keyId);

        var keyName = ExtractKeyName(keyId);
        var backupResult = await _keyClient.StartBackupKeyAsync(keyName, cancellationToken);

        // Wait for backup to complete
        await backupResult.WaitForCompletionAsync(cancellationToken);

        var key = await _keyClient.GetKeyAsync(keyName, cancellationToken: cancellationToken);

        return new KeyBackupData
        {
            KeyId = keyId,
            BackupBlob = backupResult.Value,
            BackupVersion = key.Value.Properties.Version,
            BackupDate = DateTime.UtcNow,
            SourceProvider = ProviderType,
            Metadata = new Dictionary<string, string>
            {
                ["KeyName"] = keyName,
                ["KeyVaultUri"] = _keyClient.VaultUri.ToString(),
                ["Version"] = key.Value.Properties.Version
            }
        };
    }

    public async Task<string> RestoreKeyAsync(
        KeyBackupData backup,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Restoring key from backup");

        var restoreResult = await _keyClient.StartRestoreKeyAsync(backup.BackupBlob, cancellationToken);

        // Wait for restore to complete
        var restored = await restoreResult.WaitForCompletionAsync(cancellationToken);

        _logger.LogInformation("Restored key {KeyName} with ID {KeyId}",
            restored.Value.Name, restored.Value.Id);

        return restored.Value.Id.ToString();
    }

    public async Task<bool> DeleteKeyAsync(
        string keyId,
        bool permanentDelete = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var keyName = ExtractKeyName(keyId);
            var deleteOperation = await _keyClient.StartDeleteKeyAsync(keyName, cancellationToken);

            await deleteOperation.WaitForCompletionAsync(cancellationToken);

            if (permanentDelete)
            {
                await _keyClient.PurgeDeletedKeyAsync(keyName, cancellationToken);
                _logger.LogInformation("Permanently deleted key {KeyName}", keyName);
            }
            else
            {
                _logger.LogInformation("Soft deleted key {KeyName}", keyName);
            }

            // Remove from cache
            _cryptoClients.Remove(keyId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete key {KeyId}", keyId);
            return false;
        }
    }

    public async Task<MigrationResult> MigrateKeyAsync(
        string keyId,
        IHsmProvider targetProvider,
        MigrationOptions options,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Check if target is another Key Vault provider
            if (targetProvider is KeyVaultHsmProvider targetKv)
            {
                // Direct Key Vault to Key Vault migration
                return await MigrateToKeyVaultAsync(keyId, targetKv, options, cancellationToken);
            }

            // Generic migration via backup/restore
            var backup = await BackupKeyAsync(keyId, cancellationToken);
            var newKeyId = await targetProvider.RestoreKeyAsync(backup, cancellationToken);

            if (options.VerifyAfterMigration)
            {
                var testData = Encoding.UTF8.GetBytes("Migration verification");
                var signature = await SignAsync(keyId, testData, SigningAlgorithm.RS256, cancellationToken);
                var isValid = await targetProvider.VerifyAsync(newKeyId, testData, signature, SigningAlgorithm.RS256, cancellationToken);

                if (!isValid)
                    throw new InvalidOperationException("Migration verification failed");
            }

            if (options.DeleteSourceAfterMigration)
            {
                await DeleteKeyAsync(keyId, false, cancellationToken);
            }

            return new MigrationResult
            {
                Success = true,
                NewKeyId = newKeyId,
                SourceKeyId = keyId,
                MigratedAt = DateTime.UtcNow,
                Statistics = new MigrationStatistics
                {
                    Duration = DateTime.UtcNow - startTime,
                    BytesTransferred = backup.BackupBlob.Length
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate key {KeyId}", keyId);
            return new MigrationResult
            {
                Success = false,
                SourceKeyId = keyId,
                MigratedAt = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<HsmKey> GetKeyAsync(string keyId, CancellationToken cancellationToken = default)
    {
        var keyName = ExtractKeyName(keyId);
        var key = await _keyClient.GetKeyAsync(keyName, cancellationToken: cancellationToken);
        return ConvertToHsmKey(key.Value);
    }

    public async Task<IEnumerable<HsmKey>> ListKeysAsync(
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        var keys = new List<HsmKey>();

        await foreach (var keyProperties in _keyClient.GetPropertiesOfKeysAsync(cancellationToken))
        {
            if (prefix == null || keyProperties.Name.StartsWith(prefix))
            {
                var key = await _keyClient.GetKeyAsync(keyProperties.Name, cancellationToken: cancellationToken);
                keys.Add(ConvertToHsmKey(key.Value));
            }
        }

        return keys;
    }

    public HsmProviderConfiguration GetConfiguration()
    {
        return new HsmProviderConfiguration
        {
            ProviderType = ProviderType,
            ConnectionString = _keyClient.VaultUri.ToString(),
            Settings = new Dictionary<string, string>
            {
                ["Tier"] = "Premium",
                ["HardwareProtected"] = "true",
                ["ComplianceLevel"] = ComplianceLevel.ToString(),
                ["Region"] = _configuration["KeyVault:Region"] ?? "australiaeast"
            },
            CachingEnabled = true,
            CacheDuration = TimeSpan.FromMinutes(5)
        };
    }

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Test key creation
            var testKeyName = $"health-check-{Guid.NewGuid():N}";
            var testKey = await _keyClient.CreateRsaKeyAsync(new CreateRsaKeyOptions(testKeyName)
            {
                KeySize = 2048,
                HardwareProtected = true
            }, cancellationToken);

            // Test signing
            var cryptoClient = new AzureCrypto.CryptographyClient(testKey.Value.Id, new DefaultAzureCredential());
            var testData = Encoding.UTF8.GetBytes("Health check");
            var signResult = await cryptoClient.SignDataAsync(SignatureAlgorithm.RS256, testData, cancellationToken);
            var verifyResult = await cryptoClient.VerifyDataAsync(SignatureAlgorithm.RS256, testData, signResult.Signature, cancellationToken);

            // Cleanup
            await _keyClient.StartDeleteKeyAsync(testKeyName, cancellationToken);

            return new HealthCheckResult
            {
                IsHealthy = verifyResult.IsValid,
                Status = "Healthy",
                ResponseTime = DateTime.UtcNow - startTime,
                Metrics = new Dictionary<string, object>
                {
                    ["VaultUri"] = _keyClient.VaultUri.ToString(),
                    ["Provider"] = "Azure Key Vault Premium",
                    ["HardwareProtected"] = true
                }
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckResult
            {
                IsHealthy = false,
                Status = "Unhealthy",
                ResponseTime = DateTime.UtcNow - startTime,
                ErrorMessage = ex.Message
            };
        }
    }

    #region Private Helper Methods

    private async Task<AzureCrypto.CryptographyClient> GetCryptoClientAsync(string keyId, CancellationToken cancellationToken)
    {
        if (_cryptoClients.TryGetValue(keyId, out var client))
            return client;

        var keyName = ExtractKeyName(keyId);
        var key = await _keyClient.GetKeyAsync(keyName, cancellationToken: cancellationToken);

        client = new AzureCrypto.CryptographyClient(key.Value.Id, new DefaultAzureCredential());
        _cryptoClients[keyId] = client;

        return client;
    }

    private string ExtractKeyName(string keyId)
    {
        // Handle various key ID formats
        if (keyId.StartsWith("https://"))
        {
            var uri = new Uri(keyId);
            var segments = uri.Segments;
            return segments.Length >= 3 ? segments[2].TrimEnd('/') : keyId;
        }

        return keyId;
    }

    private HsmKey ConvertToHsmKey(KeyVaultKey key)
    {
        return new HsmKey
        {
            Id = key.Id.ToString(),
            Name = key.Name,
            Type = key.KeyType switch
            {
                KeyVaultKeyType.Rsa or KeyVaultKeyType.RsaHsm => KeyType.RSA,
                KeyVaultKeyType.Ec or KeyVaultKeyType.EcHsm => KeyType.EC,
                _ => KeyType.RSA
            },
            KeySize = key.Key?.N?.Length * 8 ?? 2048,
            Usage = ConvertKeyOperationsToUsage(key.KeyOperations),
            IsHardwareBacked = key.Properties.HardwareProtected ?? false,
            CreatedOn = key.Properties.CreatedOn ?? DateTime.UtcNow,
            ExpiresOn = key.Properties.ExpiresOn,
            LastUsedOn = key.Properties.UpdatedOn,
            Version = key.Properties.Version,
            Enabled = key.Properties.Enabled ?? false,
            Tags = key.Properties.Tags ?? new Dictionary<string, string>(),
            PublicKey = key.Key != null ? Convert.ToBase64String(key.Key.ToRSA().ExportRSAPublicKey()) : null
        };
    }

    private KeyUsage ConvertKeyOperationsToUsage(IReadOnlyList<KeyOperation> operations)
    {
        var usage = KeyUsage.None;

        foreach (var op in operations)
        {
            if (op == KeyOperation.Sign) usage |= KeyUsage.Sign;
            if (op == KeyOperation.Verify) usage |= KeyUsage.Verify;
            if (op == KeyOperation.Encrypt) usage |= KeyUsage.Encrypt;
            if (op == KeyOperation.Decrypt) usage |= KeyUsage.Decrypt;
            if (op == KeyOperation.WrapKey) usage |= KeyUsage.WrapKey;
            if (op == KeyOperation.UnwrapKey) usage |= KeyUsage.UnwrapKey;
        }

        return usage;
    }

    private async Task<MigrationResult> MigrateToKeyVaultAsync(
        string keyId,
        KeyVaultHsmProvider targetProvider,
        MigrationOptions options,
        CancellationToken cancellationToken)
    {
        // Optimized Key Vault to Key Vault migration
        var startTime = DateTime.UtcNow;
        var keyName = ExtractKeyName(keyId);

        // Backup from source
        var backupOperation = await _keyClient.StartBackupKeyAsync(keyName, cancellationToken);
        var backup = await backupOperation.WaitForCompletionAsync(cancellationToken);

        // Restore to target
        var restoreOperation = await targetProvider._keyClient.StartRestoreKeyAsync(backup, cancellationToken);
        var restored = await restoreOperation.WaitForCompletionAsync(cancellationToken);

        if (options.DeleteSourceAfterMigration)
        {
            await DeleteKeyAsync(keyId, false, cancellationToken);
        }

        return new MigrationResult
        {
            Success = true,
            NewKeyId = restored.Value.Id.ToString(),
            SourceKeyId = keyId,
            MigratedAt = DateTime.UtcNow,
            Statistics = new MigrationStatistics
            {
                Duration = DateTime.UtcNow - startTime,
                BytesTransferred = backup.Length,
                OperationsPerformed = 2
            }
        };
    }

    #endregion
}