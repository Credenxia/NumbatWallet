using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using AzureCrypto = Azure.Security.KeyVault.Keys.Cryptography;
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
    private readonly ILogger<KeyVaultHsmProvider> _logger;
    private readonly IConfiguration _configuration;
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

        _logger.LogInformation("Key Vault HSM Provider initialized with URI: {Uri}", keyVaultUri);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test connectivity by listing keys (with limit of 1)
            await foreach (var keyProps in _keyClient.GetPropertiesOfKeysAsync(cancellationToken))
            {
                break; // Just checking if we can access at least one key
            }
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

        KeyVaultKey keyResponse;

        switch (request.KeyType)
        {
            case Domain.Interfaces.KeyType.RSA:
                var rsaOptions = new CreateRsaKeyOptions(request.KeyName, hardwareProtected: true)
                {
                    KeySize = request.KeySize,
                    ExpiresOn = request.ExpiresOn,
                    Enabled = true
                };
                ConfigureKeyOperations(rsaOptions, request.Usage);
                foreach (var tag in request.Tags)
                {
                    rsaOptions.Tags.Add(tag.Key, tag.Value);
                }
                keyResponse = await _keyClient.CreateRsaKeyAsync(rsaOptions, cancellationToken);
                break;

            case Domain.Interfaces.KeyType.EC:
                var ecOptions = new CreateEcKeyOptions(request.KeyName, hardwareProtected: true)
                {
                    CurveName = KeyCurveName.P256,
                    ExpiresOn = request.ExpiresOn,
                    Enabled = true
                };
                ConfigureKeyOperations(ecOptions, request.Usage);
                foreach (var tag in request.Tags)
                {
                    ecOptions.Tags.Add(tag.Key, tag.Value);
                }
                keyResponse = await _keyClient.CreateEcKeyAsync(ecOptions, cancellationToken);
                break;

            default:
                throw new NotSupportedException($"Key type {request.KeyType} not supported in Key Vault");
        }

        return ConvertToHsmKey(keyResponse);
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
            SigningAlgorithm.RS256 => AzureCrypto.SignatureAlgorithm.RS256,
            SigningAlgorithm.RS384 => AzureCrypto.SignatureAlgorithm.RS384,
            SigningAlgorithm.RS512 => AzureCrypto.SignatureAlgorithm.RS512,
            SigningAlgorithm.PS256 => AzureCrypto.SignatureAlgorithm.PS256,
            SigningAlgorithm.PS384 => AzureCrypto.SignatureAlgorithm.PS384,
            SigningAlgorithm.PS512 => AzureCrypto.SignatureAlgorithm.PS512,
            SigningAlgorithm.ES256 => AzureCrypto.SignatureAlgorithm.ES256,
            SigningAlgorithm.ES384 => AzureCrypto.SignatureAlgorithm.ES384,
            SigningAlgorithm.ES512 => AzureCrypto.SignatureAlgorithm.ES512,
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
            SigningAlgorithm.RS256 => AzureCrypto.SignatureAlgorithm.RS256,
            SigningAlgorithm.RS384 => AzureCrypto.SignatureAlgorithm.RS384,
            SigningAlgorithm.RS512 => AzureCrypto.SignatureAlgorithm.RS512,
            SigningAlgorithm.PS256 => AzureCrypto.SignatureAlgorithm.PS256,
            SigningAlgorithm.PS384 => AzureCrypto.SignatureAlgorithm.PS384,
            SigningAlgorithm.PS512 => AzureCrypto.SignatureAlgorithm.PS512,
            SigningAlgorithm.ES256 => AzureCrypto.SignatureAlgorithm.ES256,
            SigningAlgorithm.ES384 => AzureCrypto.SignatureAlgorithm.ES384,
            SigningAlgorithm.ES512 => AzureCrypto.SignatureAlgorithm.ES512,
            _ => throw new NotSupportedException($"Signing algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.VerifyDataAsync(signAlgorithm, data, signature, cancellationToken);
        return result.IsValid;
    }

    public async Task<byte[]> EncryptAsync(
        string keyId,
        byte[] plaintext,
        EncryptionAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        var cryptoClient = await GetCryptoClientAsync(keyId, cancellationToken);

        var encryptAlgorithm = algorithm switch
        {
            EncryptionAlgorithm.RSA_OAEP => AzureCrypto.EncryptionAlgorithm.RsaOaep,
            EncryptionAlgorithm.RSA_OAEP_256 => AzureCrypto.EncryptionAlgorithm.RsaOaep256,
            _ => throw new NotSupportedException($"Encryption algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.EncryptAsync(encryptAlgorithm, plaintext, cancellationToken);
        return result.Ciphertext;
    }

    public async Task<byte[]> DecryptAsync(
        string keyId,
        byte[] ciphertext,
        EncryptionAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        var cryptoClient = await GetCryptoClientAsync(keyId, cancellationToken);

        var encryptAlgorithm = algorithm switch
        {
            EncryptionAlgorithm.RSA_OAEP => AzureCrypto.EncryptionAlgorithm.RsaOaep,
            EncryptionAlgorithm.RSA_OAEP_256 => AzureCrypto.EncryptionAlgorithm.RsaOaep256,
            _ => throw new NotSupportedException($"Encryption algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.DecryptAsync(encryptAlgorithm, ciphertext, cancellationToken);
        return result.Plaintext;
    }

    public async Task<byte[]> WrapKeyAsync(
        string wrappingKeyId,
        byte[] keyToWrap,
        KeyWrapAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        var cryptoClient = await GetCryptoClientAsync(wrappingKeyId, cancellationToken);

        var wrapAlgorithm = algorithm switch
        {
            KeyWrapAlgorithm.RSA_OAEP => AzureCrypto.KeyWrapAlgorithm.RsaOaep,
            KeyWrapAlgorithm.RSA_OAEP_256 => AzureCrypto.KeyWrapAlgorithm.RsaOaep256,
            _ => throw new NotSupportedException($"Key wrap algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.WrapKeyAsync(wrapAlgorithm, keyToWrap, cancellationToken);
        return result.EncryptedKey;
    }

    public async Task<byte[]> UnwrapKeyAsync(
        string unwrappingKeyId,
        byte[] wrappedKey,
        KeyWrapAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        var cryptoClient = await GetCryptoClientAsync(unwrappingKeyId, cancellationToken);

        var wrapAlgorithm = algorithm switch
        {
            KeyWrapAlgorithm.RSA_OAEP => AzureCrypto.KeyWrapAlgorithm.RsaOaep,
            KeyWrapAlgorithm.RSA_OAEP_256 => AzureCrypto.KeyWrapAlgorithm.RsaOaep256,
            _ => throw new NotSupportedException($"Key wrap algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.UnwrapKeyAsync(wrapAlgorithm, wrappedKey, cancellationToken);
        return result.Key;
    }

    public async Task<KeyBackupData> BackupKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        // Backup/Restore operations are only available in Managed HSM, not in Key Vault Premium
        throw new NotSupportedException("Key backup is only supported in Managed HSM. Use key export/import for Key Vault Premium.");
    }

    public async Task<string> RestoreKeyAsync(
        KeyBackupData backup,
        CancellationToken cancellationToken = default)
    {
        // Backup/Restore operations are only available in Managed HSM, not in Key Vault Premium
        throw new NotSupportedException("Key restore is only supported in Managed HSM. Use key export/import for Key Vault Premium.");
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
                {
                    throw new InvalidOperationException("Migration verification failed");
                }
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
            if (prefix == null || keyProperties.Name.StartsWith(prefix, StringComparison.Ordinal))
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
            }, cancellationToken);

            // Test signing
            var cryptoClient = new AzureCrypto.CryptographyClient(testKey.Value.Id, new DefaultAzureCredential());
            var testData = Encoding.UTF8.GetBytes("Health check");
            var signResult = await cryptoClient.SignDataAsync(AzureCrypto.SignatureAlgorithm.RS256, testData, cancellationToken);
            var verifyResult = await cryptoClient.VerifyDataAsync(AzureCrypto.SignatureAlgorithm.RS256, testData, signResult.Signature, cancellationToken);

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
        {
            return client;
        }

        var keyName = ExtractKeyName(keyId);
        var key = await _keyClient.GetKeyAsync(keyName, cancellationToken: cancellationToken);

        client = new AzureCrypto.CryptographyClient(key.Value.Id, new DefaultAzureCredential());
        _cryptoClients[keyId] = client;

        return client;
    }

    private string ExtractKeyName(string keyId)
    {
        // Handle various key ID formats
        if (keyId.StartsWith("https://", StringComparison.Ordinal))
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
            Type = (key.KeyType == global::Azure.Security.KeyVault.Keys.KeyType.Rsa || key.KeyType == global::Azure.Security.KeyVault.Keys.KeyType.RsaHsm)
                ? Domain.Interfaces.KeyType.RSA
                : (key.KeyType == global::Azure.Security.KeyVault.Keys.KeyType.Ec || key.KeyType == global::Azure.Security.KeyVault.Keys.KeyType.EcHsm)
                    ? Domain.Interfaces.KeyType.EC
                    : Domain.Interfaces.KeyType.RSA,
            KeySize = key.Key?.N?.Length * 8 ?? 2048,
            Usage = ConvertKeyOperationsToUsage(key.KeyOperations.ToList()),
            IsHardwareBacked = key.KeyType == global::Azure.Security.KeyVault.Keys.KeyType.RsaHsm || key.KeyType == global::Azure.Security.KeyVault.Keys.KeyType.EcHsm || key.KeyType == global::Azure.Security.KeyVault.Keys.KeyType.OctHsm,
            CreatedOn = key.Properties.CreatedOn?.UtcDateTime ?? DateTime.UtcNow,
            ExpiresOn = key.Properties.ExpiresOn?.UtcDateTime,
            LastUsedOn = key.Properties.UpdatedOn?.UtcDateTime,
            Version = key.Properties.Version,
            Enabled = key.Properties.Enabled ?? false,
            Tags = key.Properties.Tags != null ? new Dictionary<string, string>(key.Properties.Tags) : new Dictionary<string, string>(),
            PublicKey = key.Key != null ? Convert.ToBase64String(key.Key.ToRSA().ExportRSAPublicKey()) : null
        };
    }

    private KeyUsage ConvertKeyOperationsToUsage(IReadOnlyList<KeyOperation> operations)
    {
        var usage = KeyUsage.None;

        foreach (var op in operations)
        {
            if (op == KeyOperation.Sign)
            {
                usage |= KeyUsage.Sign;
            }
            if (op == KeyOperation.Verify)
            {
                usage |= KeyUsage.Verify;
            }
            if (op == KeyOperation.Encrypt)
            {
                usage |= KeyUsage.Encrypt;
            }
            if (op == KeyOperation.Decrypt)
            {
                usage |= KeyUsage.Decrypt;
            }
            if (op == KeyOperation.WrapKey)
            {
                usage |= KeyUsage.WrapKey;
            }
            if (op == KeyOperation.UnwrapKey)
            {
                usage |= KeyUsage.UnwrapKey;
            }
        }

        return usage;
    }

    private async Task<MigrationResult> MigrateToKeyVaultAsync(
        string keyId,
        KeyVaultHsmProvider targetProvider,
        MigrationOptions options,
        CancellationToken cancellationToken)
    {
        // Backup/Restore operations are only available in Managed HSM
        // For Key Vault Premium, we need to recreate the key in the target vault
        throw new NotSupportedException("Direct migration between Key Vaults is not supported. Use Managed HSM for backup/restore operations.");
    }

    private void ConfigureKeyOperations<T>(T options, KeyUsage usage) where T : CreateKeyOptions
    {
        if (usage.HasFlag(KeyUsage.Sign))
        {
            options.KeyOperations.Add(KeyOperation.Sign);
        }
        if (usage.HasFlag(KeyUsage.Verify))
        {
            options.KeyOperations.Add(KeyOperation.Verify);
        }
        if (usage.HasFlag(KeyUsage.Encrypt))
        {
            options.KeyOperations.Add(KeyOperation.Encrypt);
        }
        if (usage.HasFlag(KeyUsage.Decrypt))
        {
            options.KeyOperations.Add(KeyOperation.Decrypt);
        }
        if (usage.HasFlag(KeyUsage.WrapKey))
        {
            options.KeyOperations.Add(KeyOperation.WrapKey);
        }
        if (usage.HasFlag(KeyUsage.UnwrapKey))
        {
            options.KeyOperations.Add(KeyOperation.UnwrapKey);
        }
    }

    #endregion
}
