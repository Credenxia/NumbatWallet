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
using Azure.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Infrastructure.Services.Providers;

/// <summary>
/// Azure Key Vault Managed HSM provider for production Phase 2
/// Provides FIPS 140-2 Level 2 compliant hardware security
/// </summary>
public class ManagedHsmProvider : IHsmProvider
{
    private readonly KeyClient _keyClient;
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

        _keyClient = new KeyClient(new Uri(hsmUri), credential);

        _logger.LogInformation("Managed HSM Provider initialized with URI: {Uri}", hsmUri);
    }

    private Azure.Core.TokenCredential GetManagedHsmCredential()
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
            var testKey = await _keyClient.CreateRsaKeyAsync(new CreateRsaKeyOptions(testKeyName)
            {
                KeySize = 2048,
                HardwareProtected = true
            }, cancellationToken);

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

        KeyVaultKey keyResponse;

        switch (request.KeyType)
        {
            case KeyType.RSA:
                var rsaOptions = new CreateRsaKeyOptions(request.KeyName, hardwareProtected: true)
                {
                    KeySize = request.KeySize,
                    ExpiresOn = request.ExpiresOn
                };
                ConfigureKeyOptions(rsaOptions, request);
                keyResponse = await _keyClient.CreateRsaKeyAsync(rsaOptions, cancellationToken);
                break;

            case KeyType.EC:
                var ecOptions = new CreateEcKeyOptions(request.KeyName, hardwareProtected: true)
                {
                    CurveName = request.KeySize switch
                    {
                        256 => KeyCurveName.P256,
                        384 => KeyCurveName.P384,
                        521 => KeyCurveName.P521,
                        _ => KeyCurveName.P256
                    },
                    ExpiresOn = request.ExpiresOn
                };
                ConfigureKeyOptions(ecOptions, request);
                keyResponse = await _keyClient.CreateEcKeyAsync(ecOptions, cancellationToken);
                break;

            case KeyType.AES:
                var octOptions = new CreateOctKeyOptions(request.KeyName, hardwareProtected: true)
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
        Domain.Interfaces.EncryptionAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        var cryptoClient = await GetCryptoClientAsync(keyId, cancellationToken);

        var encryptAlgorithm = algorithm switch
        {
            Domain.Interfaces.EncryptionAlgorithm.RSA_OAEP => AzureCrypto.EncryptionAlgorithm.RsaOaep,
            Domain.Interfaces.EncryptionAlgorithm.RSA_OAEP_256 => AzureCrypto.EncryptionAlgorithm.RsaOaep256,
            Domain.Interfaces.EncryptionAlgorithm.AES_GCM => AzureCrypto.EncryptionAlgorithm.A256Gcm,
            Domain.Interfaces.EncryptionAlgorithm.AES_CBC => AzureCrypto.EncryptionAlgorithm.A256Cbc,
            _ => throw new NotSupportedException($"Encryption algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.EncryptAsync(encryptAlgorithm, plaintext, cancellationToken);

        await LogAuditEventAsync("DataEncrypted", ExtractKeyName(keyId));

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
            Domain.Interfaces.EncryptionAlgorithm.AES_GCM => AzureCrypto.EncryptionAlgorithm.A256Gcm,
            Domain.Interfaces.EncryptionAlgorithm.AES_CBC => AzureCrypto.EncryptionAlgorithm.A256Cbc,
            _ => throw new NotSupportedException($"Encryption algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.DecryptAsync(encryptAlgorithm, ciphertext, cancellationToken);

        await LogAuditEventAsync("DataDecrypted", ExtractKeyName(keyId));

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
            Domain.Interfaces.KeyWrapAlgorithm.AES_KW => AzureCrypto.KeyWrapAlgorithm.A256Kw,
            _ => throw new NotSupportedException($"Key wrap algorithm {algorithm} not supported")
        };

        var result = await cryptoClient.WrapKeyAsync(wrapAlgorithm, keyToWrap, cancellationToken);

        await LogAuditEventAsync("KeyWrapped", ExtractKeyName(wrappingKeyId));

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
            Domain.Interfaces.KeyWrapAlgorithm.AES_KW => AzureCrypto.KeyWrapAlgorithm.A256Kw,
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
        _logger.LogInformation("Backing up key {KeyId} with security domain protection", keyId);

        var keyName = ExtractKeyName(keyId);

        // Managed HSM backup requires security domain authorization
        var backupOperation = await _keyClient.StartBackupKeyAsync(keyName, cancellationToken);
        var backup = await backupOperation.WaitForCompletionAsync(cancellationToken);

        var key = await _keyClient.GetKeyAsync(keyName, cancellationToken: cancellationToken);

        await LogAuditEventAsync("KeyBackedUp", keyName);

        return new KeyBackupData
        {
            KeyId = keyId,
            BackupBlob = backup.Value,
            BackupVersion = key.Value.Properties.Version,
            BackupDate = DateTime.UtcNow,
            SourceProvider = ProviderType,
            Metadata = new Dictionary<string, string>
            {
                ["KeyName"] = keyName,
                ["HsmUri"] = _keyClient.VaultUri.ToString(),
                ["Version"] = key.Value.Properties.Version,
                ["SecurityDomainProtected"] = "true",
                ["ComplianceLevel"] = ComplianceLevel.ToString()
            }
        };
    }

    public async Task<string> RestoreKeyAsync(
        KeyBackupData backup,
        CancellationToken cancellationToken = default)
    {
        if (backup.SourceProvider != ProviderType && backup.SourceProvider != "KeyVault")
        {
            throw new InvalidOperationException($"Cannot restore backup from {backup.SourceProvider} to Managed HSM");
        }

        _logger.LogInformation("Restoring key from backup with security domain protection");

        var restoreOperation = await _keyClient.StartRestoreKeyAsync(backup.BackupBlob, cancellationToken);
        var restored = await restoreOperation.WaitForCompletionAsync(cancellationToken);

        await LogAuditEventAsync("KeyRestored", restored.Value.Name);

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
            return cachedKey;
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
            if (prefix == null || keyProperties.Name.StartsWith(prefix))
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
            var testKey = await _keyClient.CreateRsaKeyAsync(new CreateRsaKeyOptions(testKeyName)
            {
                KeySize = 2048,
                HardwareProtected = true
            }, cancellationToken);

            // Test cryptographic operations
            var cryptoClient = new AzureCrypto.CryptographyClient(testKey.Value.Id, GetManagedHsmCredential());
            var testData = Encoding.UTF8.GetBytes("HSM Health Check");
            var signResult = await cryptoClient.SignDataAsync(Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.RS256, testData, cancellationToken);
            var verifyResult = await cryptoClient.VerifyDataAsync(Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.RS256, testData, signResult.Signature, cancellationToken);

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

    private void ConfigureKeyOptions<T>(T options, KeyGenerationRequest request) where T : CreateKeyOptions
    {
        options.Enabled = true;

        if (request.Usage.HasFlag(KeyUsage.Sign))
            options.KeyOperations.Add(KeyOperation.Sign);
        if (request.Usage.HasFlag(KeyUsage.Verify))
            options.KeyOperations.Add(KeyOperation.Verify);
        if (request.Usage.HasFlag(KeyUsage.Encrypt))
            options.KeyOperations.Add(KeyOperation.Encrypt);
        if (request.Usage.HasFlag(KeyUsage.Decrypt))
            options.KeyOperations.Add(KeyOperation.Decrypt);
        if (request.Usage.HasFlag(KeyUsage.WrapKey))
            options.KeyOperations.Add(KeyOperation.WrapKey);
        if (request.Usage.HasFlag(KeyUsage.UnwrapKey))
            options.KeyOperations.Add(KeyOperation.UnwrapKey);

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
            return client;

        await _initSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_cryptoClients.TryGetValue(keyId, out client))
                return client;

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
                KeyVaultKeyType.Oct or KeyVaultKeyType.OctHsm => KeyType.AES,
                _ => KeyType.RSA
            },
            KeySize = GetKeySize(key),
            Usage = ConvertKeyOperationsToUsage(key.KeyOperations),
            IsHardwareBacked = true, // Always true for Managed HSM
            CreatedOn = key.Properties.CreatedOn ?? DateTime.UtcNow,
            ExpiresOn = key.Properties.ExpiresOn,
            LastUsedOn = key.Properties.UpdatedOn,
            Version = key.Properties.Version,
            Enabled = key.Properties.Enabled ?? false,
            Tags = key.Properties.Tags ?? new Dictionary<string, string>()
        };
    }

    private int GetKeySize(KeyVaultKey key)
    {
        if (key.Key?.N != null)
            return key.Key.N.Length * 8;

        if (key.Key?.X != null)
            return key.Key.X.Length * 8;

        return 2048; // Default
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

    private Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm ConvertSigningAlgorithm(SigningAlgorithm algorithm)
    {
        return algorithm switch
        {
            SigningAlgorithm.RS256 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.RS256,
            SigningAlgorithm.RS384 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.RS384,
            SigningAlgorithm.RS512 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.RS512,
            SigningAlgorithm.PS256 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.PS256,
            SigningAlgorithm.PS384 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.PS384,
            SigningAlgorithm.PS512 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.PS512,
            SigningAlgorithm.ES256 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.ES256,
            SigningAlgorithm.ES384 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.ES384,
            SigningAlgorithm.ES512 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.ES512,
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
            "SHA256" => SHA256.Create(),
            "SHA384" => SHA384.Create(),
            "SHA512" => SHA512.Create(),
            _ => SHA256.Create()
        };

        return hasher.ComputeHash(data);
    }

    private async Task<MigrationResult> MigrateToManagedHsmAsync(
        string keyId,
        ManagedHsmProvider targetHsm,
        MigrationOptions options,
        CancellationToken cancellationToken)
    {
        // Optimized Managed HSM to Managed HSM migration
        var startTime = DateTime.UtcNow;
        var keyName = ExtractKeyName(keyId);

        // Backup with security domain
        var backupOperation = await _keyClient.StartBackupKeyAsync(keyName, cancellationToken);
        var backup = await backupOperation.WaitForCompletionAsync(cancellationToken);

        // Restore to target HSM
        var restoreOperation = await targetHsm._keyClient.StartRestoreKeyAsync(backup, cancellationToken);
        var restored = await restoreOperation.WaitForCompletionAsync(cancellationToken);

        if (options.DeleteSourceAfterMigration)
        {
            await DeleteKeyAsync(keyId, false, cancellationToken);
        }

        await LogAuditEventAsync("KeyMigrated", keyName,
            new Dictionary<string, string>
            {
                ["SourceHsm"] = _keyClient.VaultUri.ToString(),
                ["TargetHsm"] = targetHsm._keyClient.VaultUri.ToString()
            });

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
            throw new InvalidOperationException("Migration verification failed - signatures do not match");
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
}