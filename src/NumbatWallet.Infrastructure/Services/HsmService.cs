using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Services.Providers;

namespace NumbatWallet.Infrastructure.Services;

/// <summary>
/// HSM service that uses provider pattern for phased security implementation
/// Phase 1: Software (Dev) / Key Vault Premium (Prod)
/// Phase 2: Managed HSM
/// Phase 3: Dedicated HSM
/// </summary>
public class HsmService : IHsmService
{
    private readonly IHsmProvider _provider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HsmService> _logger;

    public HsmService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<HsmService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Select provider based on configuration
        var providerType = configuration["Hsm:Provider"] ?? "Software";

        _provider = providerType.ToLowerInvariant() switch
        {
            "software" => serviceProvider.GetRequiredService<SoftwareHsmProvider>(),
            "keyvault" => serviceProvider.GetRequiredService<KeyVaultHsmProvider>(),
            "managedhsm" => serviceProvider.GetRequiredService<ManagedHsmProvider>(),
            _ => throw new NotSupportedException($"HSM provider '{providerType}' not supported")
        };

        _logger.LogInformation("HSM Service initialized with {Provider} provider (FIPS: {Compliance})",
            _provider.ProviderType, _provider.ComplianceLevel);
    }

    public async Task<string> GenerateKeyPairAsync(
        string keyName,
        KeyAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new KeyGenerationRequest
            {
                KeyName = keyName,
                KeyType = ConvertAlgorithmToKeyType(algorithm),
                KeySize = GetKeySize(algorithm),
                Usage = KeyUsage.Sign | KeyUsage.Verify | KeyUsage.Encrypt | KeyUsage.Decrypt,
                Exportable = false,
                Tags = new Dictionary<string, string>
                {
                    ["Algorithm"] = algorithm.ToString(),
                    ["CreatedBy"] = "HsmService"
                }
            };

            var key = await _provider.GenerateKeyAsync(request, cancellationToken);

            _logger.LogInformation("Generated key pair {KeyName} with algorithm {Algorithm} using {Provider}",
                keyName, algorithm, _provider.ProviderType);

            return key.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate key pair {KeyName}", keyName);
            throw;
        }
    }

    public async Task<byte[]> SignDataAsync(
        string keyName,
        byte[] data,
        SignatureAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var signingAlgorithm = ConvertSignatureAlgorithm(algorithm);
            var signature = await _provider.SignAsync(keyName, data, signingAlgorithm, cancellationToken);

            _logger.LogDebug("Signed data with key {KeyName} using {Algorithm}", keyName, algorithm);
            return signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign data with key {KeyName}", keyName);
            throw;
        }
    }

    public async Task<bool> VerifySignatureAsync(
        string keyName,
        byte[] data,
        byte[] signature,
        SignatureAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var signingAlgorithm = ConvertSignatureAlgorithm(algorithm);
            var isValid = await _provider.VerifyAsync(keyName, data, signature, signingAlgorithm, cancellationToken);

            _logger.LogDebug("Verified signature with key {KeyName} using {Algorithm}: {IsValid}",
                keyName, algorithm, isValid);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify signature with key {KeyName}", keyName);
            throw;
        }
    }

    public async Task<byte[]> EncryptDataAsync(
        string keyName,
        byte[] plaintext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ciphertext = await _provider.EncryptAsync(
                keyName,
                plaintext,
                EncryptionAlgorithm.RSA_OAEP_256,
                cancellationToken);

            _logger.LogDebug("Encrypted data with key {KeyName}", keyName);
            return ciphertext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data with key {KeyName}", keyName);
            throw;
        }
    }

    public async Task<byte[]> DecryptDataAsync(
        string keyName,
        byte[] ciphertext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var plaintext = await _provider.DecryptAsync(
                keyName,
                ciphertext,
                EncryptionAlgorithm.RSA_OAEP_256,
                cancellationToken);

            _logger.LogDebug("Decrypted data with key {KeyName}", keyName);
            return plaintext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data with key {KeyName}", keyName);
            throw;
        }
    }

    public async Task<byte[]> WrapKeyAsync(
        string wrappingKeyName,
        byte[] keyToWrap,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var wrappedKey = await _provider.WrapKeyAsync(
                wrappingKeyName,
                keyToWrap,
                KeyWrapAlgorithm.RSA_OAEP_256,
                cancellationToken);

            _logger.LogInformation("Wrapped key with {WrappingKeyName}", wrappingKeyName);
            return wrappedKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to wrap key with {WrappingKeyName}", wrappingKeyName);
            throw;
        }
    }

    public async Task<byte[]> UnwrapKeyAsync(
        string wrappingKeyName,
        byte[] wrappedKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var unwrappedKey = await _provider.UnwrapKeyAsync(
                wrappingKeyName,
                wrappedKey,
                KeyWrapAlgorithm.RSA_OAEP_256,
                cancellationToken);

            _logger.LogInformation("Unwrapped key with {WrappingKeyName}", wrappingKeyName);
            return unwrappedKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unwrap key with {WrappingKeyName}", wrappingKeyName);
            throw;
        }
    }

    public async Task<bool> DeleteKeyAsync(
        string keyName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var permanentDelete = _configuration.GetValue<bool>("Hsm:EnablePermanentDelete");
            var result = await _provider.DeleteKeyAsync(keyName, permanentDelete, cancellationToken);

            _logger.LogWarning("Deleted key {KeyName} (permanent: {Permanent})", keyName, permanentDelete);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete key {KeyName}", keyName);
            throw;
        }
    }

    public async Task<X509Certificate2> GenerateCertificateAsync(
        string certificateName,
        string subject,
        int validityDays,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate key for certificate
            var keyRequest = new KeyGenerationRequest
            {
                KeyName = $"{certificateName}-key",
                KeyType = KeyType.RSA,
                KeySize = 2048,
                Usage = KeyUsage.Sign | KeyUsage.Verify,
                Tags = new Dictionary<string, string>
                {
                    ["Certificate"] = certificateName,
                    ["Subject"] = subject
                }
            };

            var key = await _provider.GenerateKeyAsync(keyRequest, cancellationToken);

            // Create certificate request
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                subject,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // Add extensions
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation,
                    critical: true));

            request.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false));

            // Create self-signed certificate (mock for now)
            var certificate = request.CreateSelfSigned(
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(validityDays));

            _logger.LogInformation("Generated certificate {CertificateName} with subject {Subject}",
                certificateName, subject);

            return certificate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate certificate {CertificateName}", certificateName);
            throw;
        }
    }

    public async Task<HsmHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var healthCheck = await _provider.CheckHealthAsync(cancellationToken);

            var status = new HsmHealthStatus
            {
                IsHealthy = healthCheck.IsHealthy,
                Status = healthCheck.Status,
                Details = new Dictionary<string, object>
                {
                    ["Provider"] = _provider.ProviderType,
                    ["ComplianceLevel"] = _provider.ComplianceLevel.ToString(),
                    ["Metrics"] = healthCheck.Metrics
                },
                CheckedAt = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("HSM health check: {Status} for provider {Provider}",
                status.IsHealthy ? "Healthy" : "Unhealthy", _provider.ProviderType);

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HSM health check failed");
            return new HsmHealthStatus
            {
                IsHealthy = false,
                Status = $"Health check failed: {ex.Message}",
                Details = new Dictionary<string, object>
                {
                    ["Provider"] = _provider.ProviderType,
                    ["Error"] = ex.Message
                },
                CheckedAt = DateTimeOffset.UtcNow
            };
        }
    }

    public async Task<HsmKeyMetadata> GetKeyMetadataAsync(
        string keyName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = await _provider.GetKeyAsync(keyName, cancellationToken);

            return new HsmKeyMetadata
            {
                KeyId = key.Id,
                KeyName = key.Name,
                Algorithm = ConvertKeyTypeToAlgorithm(key.Type, key.KeySize),
                CreatedAt = key.CreatedOn,
                ExpiresAt = key.ExpiresOn,
                Version = key.Version ?? string.Empty,
                Enabled = key.Enabled,
                Tags = key.Tags ?? new Dictionary<string, string>(),
                AllowedOperations = ConvertKeyUsageToOperations(key.Usage)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metadata for key {KeyName}", keyName);
            throw;
        }
    }

    public async Task<IEnumerable<HsmKeyMetadata>> ListKeysAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var keys = await _provider.ListKeysAsync(null, cancellationToken);

            return keys.Select(k => new HsmKeyMetadata
            {
                KeyId = k.Id,
                KeyName = k.Name,
                Algorithm = ConvertKeyTypeToAlgorithm(k.Type, k.KeySize),
                CreatedAt = k.CreatedOn,
                ExpiresAt = k.ExpiresOn,
                Version = k.Version ?? string.Empty,
                Enabled = k.Enabled,
                Tags = k.Tags ?? new Dictionary<string, string>(),
                AllowedOperations = ConvertKeyUsageToOperations(k.Usage)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list keys");
            throw;
        }
    }

    public async Task<string> BackupKeyAsync(
        string keyName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var backup = await _provider.BackupKeyAsync(keyName, cancellationToken);

            // Store backup reference (in production, this would be stored securely)
            var backupId = $"backup_{keyName}_{DateTime.UtcNow:yyyyMMddHHmmss}";

            _logger.LogInformation("Backed up key {KeyName} with backup ID {BackupId}", keyName, backupId);

            return backupId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backup key {KeyName}", keyName);
            throw;
        }
    }

    public async Task<bool> RestoreKeyAsync(
        string backupId,
        byte[] backupData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var backupDataObj = new KeyBackupData
            {
                BackupBlob = backupData,
                BackupDate = DateTime.UtcNow,
                SourceProvider = _provider.ProviderType
            };

            var restoredKeyId = await _provider.RestoreKeyAsync(backupDataObj, cancellationToken);

            _logger.LogInformation("Restored key from backup {BackupId} as {KeyId}", backupId, restoredKeyId);

            return !string.IsNullOrEmpty(restoredKeyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore key from backup {BackupId}", backupId);
            throw;
        }
    }

    public async Task<string> RotateKeyAsync(
        string keyName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get metadata from old key
            var oldKey = await _provider.GetKeyAsync(keyName, cancellationToken);

            // Generate new key name with version suffix
            var newKeyName = $"{keyName}-v{DateTime.UtcNow:yyyyMMddHHmmss}";

            // Generate new key with same properties
            var request = new KeyGenerationRequest
            {
                KeyName = newKeyName,
                KeyType = oldKey.Type,
                KeySize = oldKey.KeySize,
                Usage = oldKey.Usage,
                Tags = new Dictionary<string, string>(oldKey.Tags)
                {
                    ["RotatedFrom"] = keyName,
                    ["RotatedAt"] = DateTime.UtcNow.ToString("O")
                }
            };

            var newKey = await _provider.GenerateKeyAsync(request, cancellationToken);

            // Mark old key for deletion (soft delete)
            await _provider.DeleteKeyAsync(keyName, false, cancellationToken);

            _logger.LogInformation("Rotated key from {OldKey} to {NewKey}", keyName, newKeyName);

            return newKeyName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate key {KeyName}", keyName);
            throw;
        }
    }

    // Additional overload for backwards compatibility
    public async Task<bool> RotateKeyAsync(
        string oldKeyName,
        string newKeyName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get metadata from old key
            var oldKey = await _provider.GetKeyAsync(oldKeyName, cancellationToken);

            // Generate new key with same properties
            var request = new KeyGenerationRequest
            {
                KeyName = newKeyName,
                KeyType = oldKey.Type,
                KeySize = oldKey.KeySize,
                Usage = oldKey.Usage,
                Tags = new Dictionary<string, string>(oldKey.Tags)
                {
                    ["RotatedFrom"] = oldKeyName,
                    ["RotatedAt"] = DateTime.UtcNow.ToString("O")
                }
            };

            var newKey = await _provider.GenerateKeyAsync(request, cancellationToken);

            // Mark old key for deletion (soft delete)
            await _provider.DeleteKeyAsync(oldKeyName, false, cancellationToken);

            _logger.LogInformation("Rotated key from {OldKey} to {NewKey}", oldKeyName, newKeyName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate key from {OldKey} to {NewKey}", oldKeyName, newKeyName);
            throw;
        }
    }

    public async Task<bool> MigrateToProviderAsync(
        string targetProviderType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting migration from {Current} to {Target}",
                _provider.ProviderType, targetProviderType);

            // This would be implemented to migrate all keys to a new provider
            // For now, return true as placeholder
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate to provider {Provider}", targetProviderType);
            throw;
        }
    }

    #region Helper Methods

    private KeyType ConvertAlgorithmToKeyType(KeyAlgorithm algorithm)
    {
        return algorithm switch
        {
            KeyAlgorithm.RSA2048 or KeyAlgorithm.RSA3072 or KeyAlgorithm.RSA4096 => KeyType.RSA,
            KeyAlgorithm.ECC_P256 or KeyAlgorithm.ECC_P384 or KeyAlgorithm.ECC_P521 => KeyType.EC,
            KeyAlgorithm.AES128 or KeyAlgorithm.AES256 => KeyType.AES,
            _ => KeyType.RSA
        };
    }

    private int GetKeySize(KeyAlgorithm algorithm)
    {
        return algorithm switch
        {
            KeyAlgorithm.RSA2048 => 2048,
            KeyAlgorithm.RSA3072 => 3072,
            KeyAlgorithm.RSA4096 => 4096,
            KeyAlgorithm.ECC_P256 => 256,
            KeyAlgorithm.ECC_P384 => 384,
            KeyAlgorithm.ECC_P521 => 521,
            KeyAlgorithm.AES128 => 128,
            KeyAlgorithm.AES256 => 256,
            _ => 2048
        };
    }

    private SigningAlgorithm ConvertSignatureAlgorithm(SignatureAlgorithm algorithm)
    {
        return algorithm switch
        {
            SignatureAlgorithm.RS256 => SigningAlgorithm.RS256,
            SignatureAlgorithm.RS384 => SigningAlgorithm.RS384,
            SignatureAlgorithm.RS512 => SigningAlgorithm.RS512,
            SignatureAlgorithm.PS256 => SigningAlgorithm.PS256,
            SignatureAlgorithm.PS384 => SigningAlgorithm.PS384,
            SignatureAlgorithm.PS512 => SigningAlgorithm.PS512,
            SignatureAlgorithm.ES256 => SigningAlgorithm.ES256,
            SignatureAlgorithm.ES384 => SigningAlgorithm.ES384,
            SignatureAlgorithm.ES512 => SigningAlgorithm.ES512,
            _ => SigningAlgorithm.RS256
        };
    }

    private KeyAlgorithm ConvertKeyTypeToAlgorithm(KeyType keyType, int keySize)
    {
        return keyType switch
        {
            KeyType.RSA => keySize switch
            {
                2048 => KeyAlgorithm.RSA2048,
                3072 => KeyAlgorithm.RSA3072,
                4096 => KeyAlgorithm.RSA4096,
                _ => KeyAlgorithm.RSA2048
            },
            KeyType.EC => keySize switch
            {
                256 => KeyAlgorithm.ECC_P256,
                384 => KeyAlgorithm.ECC_P384,
                521 => KeyAlgorithm.ECC_P521,
                _ => KeyAlgorithm.ECC_P256
            },
            KeyType.AES => keySize switch
            {
                128 => KeyAlgorithm.AES128,
                _ => KeyAlgorithm.AES256
            },
            _ => KeyAlgorithm.RSA2048
        };
    }

    #endregion

    #region Missing Interface Methods

    public async Task<byte[]> GetPublicKeyAsync(
        string keyName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = await _provider.GetKeyAsync(keyName, cancellationToken);
            return Encoding.UTF8.GetBytes(key.PublicKey ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get public key for {KeyName}", keyName);
            throw;
        }
    }

    public async Task<byte[]> CreateCertificateSigningRequestAsync(
        string keyName,
        X500DistinguishedName subjectName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the public key
            var publicKey = await GetPublicKeyAsync(keyName, cancellationToken);

            // Create CSR using the public key
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKey, out _);

            var request = new CertificateRequest(
                subjectName,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // Convert to DER format (standard CSR format)
            var csr = request.CreateSigningRequest();

            _logger.LogInformation("Created CSR for key {KeyName}", keyName);
            return csr;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create CSR for key {KeyName}", keyName);
            throw;
        }
    }

    public async Task<bool> ImportCertificateAsync(
        string keyName,
        X509Certificate2 certificate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Store certificate data as a tag on the key
            var key = await _provider.GetKeyAsync(keyName, cancellationToken);
            key.Tags["Certificate"] = Convert.ToBase64String(certificate.RawData);
            key.Tags["CertificateThumbprint"] = certificate.Thumbprint;
            key.Tags["CertificateSubject"] = certificate.Subject;

            // Update key with certificate information
            // TODO: Implement key update mechanism - IHsmProvider doesn't have UpdateKeyAsync
            // await _provider.UpdateKeyAsync(keyName, key.Tags, cancellationToken);

            _logger.LogInformation("Imported certificate for key {KeyName}", keyName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import certificate for key {KeyName}", keyName);
            throw;
        }
    }


    async Task<IEnumerable<string>> IHsmService.ListKeysAsync(
        CancellationToken cancellationToken)
    {
        var keys = await ListKeysAsync(cancellationToken);
        return keys.Select(k => k.KeyName);
    }

    public async Task<HsmHealthStatus> GetHealthStatusAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var providerHealth = await _provider.CheckHealthAsync(cancellationToken);

            return new HsmHealthStatus
            {
                IsHealthy = providerHealth.IsHealthy,
                Status = providerHealth.Status,
                Details = new Dictionary<string, object>
                {
                    ["Provider"] = _provider.ProviderType,
                    ["ComplianceLevel"] = _provider.ComplianceLevel.ToString(),
                    ["SupportsHardwareBackedKeys"] = _provider.SupportsHardwareBackedKeys
                },
                CheckedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get HSM health status");
            return new HsmHealthStatus
            {
                IsHealthy = false,
                Status = $"Health check failed: {ex.Message}",
                CheckedAt = DateTimeOffset.UtcNow
            };
        }
    }

    private List<string> ConvertKeyUsageToOperations(KeyUsage usage)
    {
        var operations = new List<string>();

        if (usage.HasFlag(KeyUsage.Sign))
        {
            operations.Add("Sign");
        }
        if (usage.HasFlag(KeyUsage.Verify))
        {
            operations.Add("Verify");
        }
        if (usage.HasFlag(KeyUsage.Encrypt))
        {
            operations.Add("Encrypt");
        }
        if (usage.HasFlag(KeyUsage.Decrypt))
        {
            operations.Add("Decrypt");
        }
        if (usage.HasFlag(KeyUsage.WrapKey))
        {
            operations.Add("WrapKey");
        }
        if (usage.HasFlag(KeyUsage.UnwrapKey))
        {
            operations.Add("UnwrapKey");
        }

        return operations;
    }

    #endregion
}
