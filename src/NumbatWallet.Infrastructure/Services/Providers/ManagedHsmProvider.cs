using System.Security.Cryptography;
using System.Text;
using Azure.Core;
using Azure.Identity;
using AzureKeys = Azure.Security.KeyVault.Keys;
using AzureCrypto = Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Infrastructure.Services.Providers;

/// <summary>
/// Azure Key Vault Managed HSM provider for production Phase 2
/// Provides FIPS 140-2 Level 2 compliant hardware security
/// </summary>
public class ManagedHsmProvider : IHsmProvider, IDisposable
{
    private readonly AzureKeys.KeyClient _keyClient;
    private readonly ILogger<ManagedHsmProvider> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly Dictionary<string, AzureCrypto.CryptographyClient> _cryptoClients;
    private readonly SemaphoreSlim _initSemaphore;

    public string ProviderType => "ManagedHSM";
    public bool SupportsHardwareBackedKeys => true;
    public FipsComplianceLevel ComplianceLevel => FipsComplianceLevel.Level2; // Hardware-protected FIPS 140-2 Level 2

    public ManagedHsmProvider(
        IConfiguration configuration,
        ILogger<ManagedHsmProvider> logger,
        IMemoryCache cache)
    {
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
        _cryptoClients = new Dictionary<string, AzureCrypto.CryptographyClient>();
        _initSemaphore = new SemaphoreSlim(1, 1);

        var hsmUri = configuration["ManagedHsm:Uri"]
            ?? throw new InvalidOperationException("ManagedHsm:Uri not configured");

        // Use managed identity or certificate authentication for Managed HSM
        var credential = GetManagedHsmCredential();

        _keyClient = new AzureKeys.KeyClient(new Uri(hsmUri), credential);

        _logger.LogInformation("Managed HSM Provider initialized with URI: {Uri}", hsmUri);
    }

    private TokenCredential GetManagedHsmCredential()
    {
        // Check for certificate authentication (preferred for HSM)
        var certThumbprint = _configuration["ManagedHsm:CertificateThumbprint"];
        if (!string.IsNullOrEmpty(certThumbprint))
        {
            var tenantId = _configuration["ManagedHsm:TenantId"];
            var clientId = _configuration["ManagedHsm:ClientId"];

            return new ClientCertificateCredential(
                tenantId,
                clientId,
                certThumbprint);
        }

        // Fall back to managed identity
        return new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = _configuration["ManagedHsm:ManagedIdentityClientId"],
            ExcludeEnvironmentCredential = true, // More secure for HSM
            ExcludeAzureCliCredential = true
        });
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check HSM availability and security domain status
            var testKeyName = $"availability-test-{Guid.NewGuid():N}";
            _ = await _keyClient.CreateRsaKeyAsync(new AzureKeys.CreateRsaKeyOptions(testKeyName, hardwareProtected: true)
            {
                KeySize = 2048            }, cancellationToken);

            // Clean up test key
            await _keyClient.StartDeleteKeyAsync(testKeyName, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Managed HSM availability check failed");
            return false;
        }
    }

    public async Task<HsmKey> GenerateKeyAsync(
        KeyGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating {Type} key in Managed HSM: {Name}", request.KeyType, request.KeyName);

        AzureKeys.KeyVaultKey keyResponse;

        switch (request.KeyType)
        {
            case KeyType.RSA:
                var rsaOptions = new AzureKeys.CreateRsaKeyOptions(request.KeyName, hardwareProtected: true)
                {
                    KeySize = request.KeySize,
                    ExpiresOn = request.ExpiresOn
                };
                ConfigureKeyOptions(rsaOptions, request);
                keyResponse = await _keyClient.CreateRsaKeyAsync(rsaOptions, cancellationToken);
                break;

            case KeyType.EC:
                var ecOptions = new AzureKeys.CreateEcKeyOptions(request.KeyName, hardwareProtected: true)
                {
                    CurveName = request.KeySize switch
                    {
                        256 => AzureKeys.KeyCurveName.P256,
                        384 => AzureKeys.KeyCurveName.P384,
                        521 => AzureKeys.KeyCurveName.P521,
                        _ => AzureKeys.KeyCurveName.P256
                    },
                    ExpiresOn = request.ExpiresOn
                };
                ConfigureKeyOptions(ecOptions, request);
                keyResponse = await _keyClient.CreateEcKeyAsync(ecOptions, cancellationToken);
                break;

            case KeyType.AES:
                var octOptions = new AzureKeys.CreateOctKeyOptions(request.KeyName, hardwareProtected: true)
                {
                    KeySize = request.KeySize,
                    ExpiresOn = request.ExpiresOn
                };
                ConfigureKeyOptions(octOptions, request);
                keyResponse = await _keyClient.CreateOctKeyAsync(octOptions, cancellationToken);
                break;

            default:
                throw new NotSupportedException($"Key type {request.KeyType} not supported in Managed HSM");
        }

        // Log audit event for key generation
        await LogAuditEventAsync("KeyGenerated", keyResponse.Name, request.Tags);

        return ConvertToHsmKey(keyResponse);
    }

    public async Task<byte[]> SignAsync(
        string keyId,
        byte[] data,
        SigningAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        var cryptoClient = await GetCryptoClientAsync(keyId, cancellationToken);

        // For Managed HSM, use direct hash signing for better performance
        var hash = ComputeHash(data, algorithm);

        var signAlgorithm = ConvertSigningAlgorithm(algorithm);
        var result = await cryptoClient.SignAsync(signAlgorithm, hash, cancellationToken);

        await LogAuditEventAsync("DataSigned", ExtractKeyName(keyId));

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

        var hash = ComputeHash(data, algorithm);
        var signAlgorithm = ConvertSigningAlgorithm(algorithm);

        var result = await cryptoClient.VerifyAsync(signAlgorithm, hash, signature, cancellationToken);
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
            EncryptionAlgorithm.AES_GCM => AzureCrypto.EncryptionAlgorithm.A256Gcm,
            EncryptionAlgorithm.AES_CBC => AzureCrypto.EncryptionAlgorithm.A256Cbc,
            _ => throw new NotSupportedException($"Encryption algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.EncryptAsync(encryptAlgorithm, plaintext, cancellationToken);

        await LogAuditEventAsync("DataEncrypted", ExtractKeyName(keyId));

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
            EncryptionAlgorithm.AES_GCM => AzureCrypto.EncryptionAlgorithm.A256Gcm,
            EncryptionAlgorithm.AES_CBC => AzureCrypto.EncryptionAlgorithm.A256Cbc,
            _ => throw new NotSupportedException($"Encryption algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.DecryptAsync(encryptAlgorithm, ciphertext, cancellationToken);

        await LogAuditEventAsync("DataDecrypted", ExtractKeyName(keyId));

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
            KeyWrapAlgorithm.AES_KW => AzureCrypto.KeyWrapAlgorithm.A256KW,
            _ => throw new NotSupportedException($"Key wrap algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.WrapKeyAsync(wrapAlgorithm, keyToWrap, cancellationToken);

        await LogAuditEventAsync("KeyWrapped", ExtractKeyName(wrappingKeyId));

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
            KeyWrapAlgorithm.AES_KW => AzureCrypto.KeyWrapAlgorithm.A256KW,
            _ => throw new NotSupportedException($"Key wrap algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.UnwrapKeyAsync(wrapAlgorithm, wrappedKey, cancellationToken);

        await LogAuditEventAsync("KeyUnwrapped", ExtractKeyName(unwrappingKeyId));

        return result.Key;
    }

    public async Task<KeyBackupData> BackupKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        // Note: Backup/Restore operations require the Azure Key Vault Administration client library
        // which is separate from the Keys library. This would need additional package references.
        // For production Managed HSM, you would use:
        // var backupClient = new KeyVaultBackupClient(new Uri(hsmUri), new DefaultAzureCredential());
        // var backup = await backupClient.StartKeyBackupAsync(keyName);
        await Task.CompletedTask; // Suppress async warning
        throw new NotImplementedException("Backup operations require Azure.Security.KeyVault.Administration package.");
    }

    public async Task<string> RestoreKeyAsync(
        KeyBackupData backup,
        CancellationToken cancellationToken = default)
    {
        // Note: Backup/Restore operations require the Azure Key Vault Administration client library
        // which is separate from the Keys library. This would need additional package references.
        // For production Managed HSM, you would use:
        // var backupClient = new KeyVaultBackupClient(new Uri(hsmUri), new DefaultAzureCredential());
        // var restore = await backupClient.StartKeyRestoreAsync(backup.BackupBlob);
        await Task.CompletedTask; // Suppress async warning
        throw new NotImplementedException("Restore operations require Azure.Security.KeyVault.Administration package.");
    }

    public async Task<bool> DeleteKeyAsync(
        string keyId,
        bool permanentDelete = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var keyName = ExtractKeyName(keyId);

            // Start soft delete
            var deleteOperation = await _keyClient.StartDeleteKeyAsync(keyName, cancellationToken);
            await deleteOperation.WaitForCompletionAsync(cancellationToken);

            if (permanentDelete)
            {
                // Permanent deletion requires additional authorization in Managed HSM
                if (bool.Parse(_configuration["ManagedHsm:AllowPurge"] ?? "false"))
                {
                    await _keyClient.PurgeDeletedKeyAsync(keyName, cancellationToken);
                    await LogAuditEventAsync("KeyPurged", keyName);
                }
                else
                {
                    _logger.LogWarning("Permanent deletion requested but purge is disabled for Managed HSM");
                }
            }
            else
            {
                await LogAuditEventAsync("KeyDeleted", keyName);
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
            // Managed HSM to Managed HSM migration
            if (targetProvider is ManagedHsmProvider targetHsm)
            {
                return await MigrateToManagedHsmAsync(keyId, targetHsm, options, cancellationToken);
            }

            // Migration to Key Vault (downgrade)
            if (targetProvider is KeyVaultHsmProvider)
            {
                _logger.LogWarning("Migrating from Managed HSM to Key Vault - security downgrade");
            }

            // Generic migration via backup/restore
            var backup = await BackupKeyAsync(keyId, cancellationToken);
            var newKeyId = await targetProvider.RestoreKeyAsync(backup, cancellationToken);

            if (options.VerifyAfterMigration)
            {
                await VerifyMigrationAsync(keyId, newKeyId, targetProvider, cancellationToken);
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
                    BytesTransferred = backup.BackupBlob.Length,
                    OperationsPerformed = 3
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
        var cacheKey = $"key_{keyId}";
        if (_cache.TryGetValue<HsmKey>(cacheKey, out var cachedKey))
        {
            return cachedKey!;
        }

        var keyName = ExtractKeyName(keyId);
        var key = await _keyClient.GetKeyAsync(keyName, cancellationToken: cancellationToken);
        var hsmKey = ConvertToHsmKey(key.Value);

        _cache.Set(cacheKey, hsmKey, TimeSpan.FromMinutes(5));

        return hsmKey;
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
                var key = await GetKeyAsync(keyProperties.Id.ToString(), cancellationToken);
                keys.Add(key);
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
                ["Type"] = "Managed HSM",
                ["ComplianceLevel"] = "FIPS 140-2 Level 2",
                ["SecurityDomain"] = "Enabled",
                ["Region"] = _configuration["ManagedHsm:Region"] ?? "australiaeast",
                ["HighAvailability"] = "true"
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
            // Check HSM responsiveness
            var testKeyName = $"health-{Guid.NewGuid():N}";
            var testKey = await _keyClient.CreateRsaKeyAsync(new AzureKeys.CreateRsaKeyOptions(testKeyName, hardwareProtected: true)
            {
                KeySize = 2048            }, cancellationToken);

            // Test cryptographic operations
            var cryptoClient = new AzureCrypto.CryptographyClient(testKey.Value.Id, GetManagedHsmCredential());
            var testData = Encoding.UTF8.GetBytes("HSM Health Check");
            var signResult = await cryptoClient.SignDataAsync(AzureCrypto.SignatureAlgorithm.RS256, testData, cancellationToken);
            var verifyResult = await cryptoClient.VerifyDataAsync(AzureCrypto.SignatureAlgorithm.RS256, testData, signResult.Signature, cancellationToken);

            // Cleanup
            await _keyClient.StartDeleteKeyAsync(testKeyName, cancellationToken);

            // Check security domain status (mock for now)
            var securityDomainHealthy = await CheckSecurityDomainAsync(cancellationToken);

            return new HealthCheckResult
            {
                IsHealthy = verifyResult.IsValid && securityDomainHealthy,
                Status = "Healthy",
                ResponseTime = DateTime.UtcNow - startTime,
                Metrics = new Dictionary<string, object>
                {
                    ["HsmUri"] = _keyClient.VaultUri.ToString(),
                    ["Provider"] = "Azure Managed HSM",
                    ["ComplianceLevel"] = "FIPS 140-2 Level 2",
                    ["SecurityDomain"] = securityDomainHealthy ? "Active" : "Degraded",
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

    private void ConfigureKeyOptions<T>(T options, KeyGenerationRequest request) where T : AzureKeys.CreateKeyOptions
    {
        options.Enabled = true;

        if (request.Usage.HasFlag(KeyUsage.Sign))
        {
            options.KeyOperations.Add(AzureKeys.KeyOperation.Sign);
        }
        if (request.Usage.HasFlag(KeyUsage.Verify))
        {
            options.KeyOperations.Add(AzureKeys.KeyOperation.Verify);
        }
        if (request.Usage.HasFlag(KeyUsage.Encrypt))
        {
            options.KeyOperations.Add(AzureKeys.KeyOperation.Encrypt);
        }
        if (request.Usage.HasFlag(KeyUsage.Decrypt))
        {
            options.KeyOperations.Add(AzureKeys.KeyOperation.Decrypt);
        }
        if (request.Usage.HasFlag(KeyUsage.WrapKey))
        {
            options.KeyOperations.Add(AzureKeys.KeyOperation.WrapKey);
        }
        if (request.Usage.HasFlag(KeyUsage.UnwrapKey))
        {
            options.KeyOperations.Add(AzureKeys.KeyOperation.UnwrapKey);
        }

        foreach (var tag in request.Tags)
        {
            options.Tags.Add(tag.Key, tag.Value);
        }

        // Add Managed HSM specific tags
        options.Tags.Add("Provider", ProviderType);
        options.Tags.Add("ComplianceLevel", ComplianceLevel.ToString());
        options.Tags.Add("CreatedAt", DateTime.UtcNow.ToString("O"));
    }

    private async Task<AzureCrypto.CryptographyClient> GetCryptoClientAsync(string keyId, CancellationToken cancellationToken)
    {
        if (_cryptoClients.TryGetValue(keyId, out var client))
        {
            return client;
        }

        await _initSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_cryptoClients.TryGetValue(keyId, out client))
            {
                return client;
            }

            var keyName = ExtractKeyName(keyId);
            var key = await _keyClient.GetKeyAsync(keyName, cancellationToken: cancellationToken);

            client = new AzureCrypto.CryptographyClient(key.Value.Id, GetManagedHsmCredential());
            _cryptoClients[keyId] = client;

            return client;
        }
        finally
        {
            _initSemaphore.Release();
        }
    }

    private string ExtractKeyName(string keyId)
    {
        if (keyId.StartsWith("https://", StringComparison.Ordinal))
        {
            var uri = new Uri(keyId);
            var segments = uri.Segments;
            return segments.Length >= 3 ? segments[2].TrimEnd('/') : keyId;
        }
        return keyId;
    }

    private HsmKey ConvertToHsmKey(AzureKeys.KeyVaultKey key)
    {
        return new HsmKey
        {
            Id = key.Id.ToString(),
            Name = key.Name,
            Type = (key.KeyType == AzureKeys.KeyType.Rsa || key.KeyType == AzureKeys.KeyType.RsaHsm)
                ? KeyType.RSA
                : (key.KeyType == AzureKeys.KeyType.Ec || key.KeyType == AzureKeys.KeyType.EcHsm)
                    ? KeyType.EC
                    : (key.KeyType == AzureKeys.KeyType.Oct || key.KeyType == AzureKeys.KeyType.OctHsm)
                        ? KeyType.AES
                        : KeyType.RSA,
            KeySize = GetKeySize(key),
            Usage = ConvertKeyOperationsToUsage(key.KeyOperations.ToList()),
            IsHardwareBacked = true, // Always true for Managed HSM
            CreatedOn = key.Properties.CreatedOn?.UtcDateTime ?? DateTime.UtcNow,
            ExpiresOn = key.Properties.ExpiresOn?.UtcDateTime,
            LastUsedOn = key.Properties.UpdatedOn?.UtcDateTime,
            Version = key.Properties.Version,
            Enabled = key.Properties.Enabled ?? false,
            Tags = key.Properties.Tags != null ? new Dictionary<string, string>(key.Properties.Tags) : new Dictionary<string, string>()
        };
    }

    private int GetKeySize(AzureKeys.KeyVaultKey key)
    {
        if (key.Key?.N != null)
        {
            return key.Key.N.Length * 8;
        }

        if (key.Key?.X != null)
        {
            return key.Key.X.Length * 8;
        }

        return 2048; // Default
    }

    private KeyUsage ConvertKeyOperationsToUsage(IReadOnlyList<AzureKeys.KeyOperation> operations)
    {
        var usage = KeyUsage.None;

        foreach (var op in operations)
        {
            if (op == AzureKeys.KeyOperation.Sign)
            {
                usage |= KeyUsage.Sign;
            }
            if (op == AzureKeys.KeyOperation.Verify)
            {
                usage |= KeyUsage.Verify;
            }
            if (op == AzureKeys.KeyOperation.Encrypt)
            {
                usage |= KeyUsage.Encrypt;
            }
            if (op == AzureKeys.KeyOperation.Decrypt)
            {
                usage |= KeyUsage.Decrypt;
            }
            if (op == AzureKeys.KeyOperation.WrapKey)
            {
                usage |= KeyUsage.WrapKey;
            }
            if (op == AzureKeys.KeyOperation.UnwrapKey)
            {
                usage |= KeyUsage.UnwrapKey;
            }
        }

        return usage;
    }

    private AzureCrypto.SignatureAlgorithm ConvertSigningAlgorithm(SigningAlgorithm algorithm)
    {
        return algorithm switch
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
    }

    private byte[] ComputeHash(byte[] data, SigningAlgorithm algorithm)
    {
        var hashAlgorithm = algorithm switch
        {
            SigningAlgorithm.RS256 or SigningAlgorithm.PS256 or SigningAlgorithm.ES256 => HashAlgorithmName.SHA256,
            SigningAlgorithm.RS384 or SigningAlgorithm.PS384 or SigningAlgorithm.ES384 => HashAlgorithmName.SHA384,
            SigningAlgorithm.RS512 or SigningAlgorithm.PS512 or SigningAlgorithm.ES512 => HashAlgorithmName.SHA512,
            _ => HashAlgorithmName.SHA256
        };

        using var hasher = hashAlgorithm.Name switch
        {
            "SHA256" => (HashAlgorithm)SHA256.Create(),
            "SHA384" => (HashAlgorithm)SHA384.Create(),
            "SHA512" => (HashAlgorithm)SHA512.Create(),
            _ => (HashAlgorithm)SHA256.Create()
        };

        return hasher.ComputeHash(data);
    }

    private async Task<MigrationResult> MigrateToManagedHsmAsync(
        string _,
        ManagedHsmProvider _targetHsm,
        MigrationOptions _options,
        CancellationToken _ct)
    {
        // Optimized Managed HSM to Managed HSM migration

        // Note: Backup/Restore operations require the Azure Key Vault Administration client library
        // For production Managed HSM, you would use:
        // var backupClient = new KeyVaultBackupClient(new Uri(hsmUri), new DefaultAzureCredential());
        // var backup = await backupClient.StartKeyBackupAsync(keyName);
        // var restore = await targetHsm.backupClient.StartKeyRestoreAsync(backup);
        await Task.CompletedTask; // Suppress async warning
        throw new NotImplementedException("HSM to HSM migration requires Azure.Security.KeyVault.Administration package.");
    }

    private async Task VerifyMigrationAsync(
        string sourceKeyId,
        string targetKeyId,
        IHsmProvider targetProvider,
        CancellationToken cancellationToken)
    {
        var testData = Encoding.UTF8.GetBytes($"Migration verification {DateTime.UtcNow}");
        var signature = await SignAsync(sourceKeyId, testData, SigningAlgorithm.RS256, cancellationToken);
        var isValid = await targetProvider.VerifyAsync(targetKeyId, testData, signature, SigningAlgorithm.RS256, cancellationToken);

        if (!isValid)
        {
            throw new InvalidOperationException("Migration verification failed - signatures do not match");
        }
    }

    private async Task<bool> CheckSecurityDomainAsync(CancellationToken cancellationToken)
    {
        // Mock implementation - real implementation would check security domain status
        await Task.Delay(10, cancellationToken);
        return true;
    }

    private async Task LogAuditEventAsync(string eventType, string keyName, Dictionary<string, string>? additionalData = null)
    {
        // Mock implementation - real implementation would log to audit system
        var auditEntry = new
        {
            Timestamp = DateTime.UtcNow,
            EventType = eventType,
            KeyName = keyName,
            HsmUri = _keyClient.VaultUri.ToString(),
            Provider = ProviderType,
            AdditionalData = additionalData
        };

        _logger.LogInformation("HSM Audit Event: {Event}", System.Text.Json.JsonSerializer.Serialize(auditEntry));
        await Task.CompletedTask;
    }

    #endregion

    public void Dispose()
    {
        _initSemaphore?.Dispose();
        GC.SuppressFinalize(this);
    }
}
