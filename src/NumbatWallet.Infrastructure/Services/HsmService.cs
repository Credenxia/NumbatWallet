using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

/// <summary>
/// Azure Key Vault Managed HSM implementation for hardware security operations
/// </summary>
public class HsmService : IHsmService
{
    private readonly KeyClient _keyClient;
    private readonly CertificateClient _certificateClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HsmService> _logger;
    private readonly Dictionary<string, CryptographyClient> _cryptoClients;

    public HsmService(
        IConfiguration configuration,
        ILogger<HsmService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _cryptoClients = new Dictionary<string, CryptographyClient>();

        var keyVaultUri = new Uri(_configuration["AzureKeyVault:Uri"]
            ?? throw new InvalidOperationException("Azure Key Vault URI not configured"));

        var credential = new DefaultAzureCredential();
        _keyClient = new KeyClient(keyVaultUri, credential);
        _certificateClient = new CertificateClient(keyVaultUri, credential);
    }

    public async Task<string> GenerateKeyPairAsync(
        string keyName,
        KeyAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        try
        {
            KeyVaultKey keyResponse;

            switch (algorithm)
            {
                case KeyAlgorithm.RSA2048:
                case KeyAlgorithm.RSA3072:
                case KeyAlgorithm.RSA4096:
                    var rsaOptions = new CreateRsaKeyOptions(keyName)
                    {
                        KeySize = algorithm switch
                        {
                            KeyAlgorithm.RSA2048 => 2048,
                            KeyAlgorithm.RSA3072 => 3072,
                            KeyAlgorithm.RSA4096 => 4096,
                            _ => 2048
                        },
                        Enabled = true,
                        ExpiresOn = DateTimeOffset.UtcNow.AddYears(2)
                    };
                    rsaOptions.KeyOperations.Add(KeyOperation.Sign);
                    rsaOptions.KeyOperations.Add(KeyOperation.Verify);
                    rsaOptions.KeyOperations.Add(KeyOperation.Encrypt);
                    rsaOptions.KeyOperations.Add(KeyOperation.Decrypt);
                    keyResponse = await _keyClient.CreateRsaKeyAsync(rsaOptions, cancellationToken);
                    break;

                case KeyAlgorithm.ECC_P256:
                case KeyAlgorithm.ECC_P384:
                case KeyAlgorithm.ECC_P521:
                    var ecOptions = new CreateEcKeyOptions(keyName)
                    {
                        CurveName = algorithm switch
                        {
                            KeyAlgorithm.ECC_P256 => KeyCurveName.P256,
                            KeyAlgorithm.ECC_P384 => KeyCurveName.P384,
                            KeyAlgorithm.ECC_P521 => KeyCurveName.P521,
                            _ => KeyCurveName.P256
                        },
                        Enabled = true,
                        ExpiresOn = DateTimeOffset.UtcNow.AddYears(2)
                    };
                    ecOptions.KeyOperations.Add(KeyOperation.Sign);
                    ecOptions.KeyOperations.Add(KeyOperation.Verify);
                    keyResponse = await _keyClient.CreateEcKeyAsync(ecOptions, cancellationToken);
                    break;

                case KeyAlgorithm.AES128:
                case KeyAlgorithm.AES256:
                    var aesOptions = new CreateOctKeyOptions(keyName)
                    {
                        KeySize = algorithm == KeyAlgorithm.AES128 ? 128 : 256,
                        Enabled = true,
                        ExpiresOn = DateTimeOffset.UtcNow.AddYears(2)
                    };
                    aesOptions.KeyOperations.Add(KeyOperation.Encrypt);
                    aesOptions.KeyOperations.Add(KeyOperation.Decrypt);
                    aesOptions.KeyOperations.Add(KeyOperation.WrapKey);
                    aesOptions.KeyOperations.Add(KeyOperation.UnwrapKey);
                    keyResponse = await _keyClient.CreateOctKeyAsync(aesOptions, cancellationToken);
                    break;

                default:
                    throw new NotSupportedException($"Algorithm {algorithm} not supported for key generation");
            }

            _logger.LogInformation("Generated key pair {KeyName} with algorithm {Algorithm}",
                keyName, algorithm);

            return keyResponse.Key.Id.ToString();
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
        Domain.Interfaces.SignatureAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cryptoClient = await GetCryptographyClientAsync(keyName, cancellationToken);

            var signAlgorithm = algorithm switch
            {
                Domain.Interfaces.SignatureAlgorithm.RS256 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.RS256,
                Domain.Interfaces.SignatureAlgorithm.RS384 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.RS384,
                Domain.Interfaces.SignatureAlgorithm.RS512 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.RS512,
                Domain.Interfaces.SignatureAlgorithm.ES256 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.ES256,
                Domain.Interfaces.SignatureAlgorithm.ES384 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.ES384,
                Domain.Interfaces.SignatureAlgorithm.ES512 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.ES512,
                Domain.Interfaces.SignatureAlgorithm.PS256 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.PS256,
                Domain.Interfaces.SignatureAlgorithm.PS384 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.PS384,
                Domain.Interfaces.SignatureAlgorithm.PS512 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.PS512,
                _ => throw new NotSupportedException($"Signature algorithm {algorithm} not supported")
            };

            var signResult = await cryptoClient.SignDataAsync(
                signAlgorithm,
                data,
                cancellationToken);

            _logger.LogDebug("Signed data with key {KeyName} using {Algorithm}",
                keyName, algorithm);

            return signResult.Signature;
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
        Domain.Interfaces.SignatureAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cryptoClient = await GetCryptographyClientAsync(keyName, cancellationToken);

            var signAlgorithm = algorithm switch
            {
                Domain.Interfaces.SignatureAlgorithm.RS256 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.RS256,
                Domain.Interfaces.SignatureAlgorithm.RS384 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.RS384,
                Domain.Interfaces.SignatureAlgorithm.RS512 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.RS512,
                Domain.Interfaces.SignatureAlgorithm.ES256 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.ES256,
                Domain.Interfaces.SignatureAlgorithm.ES384 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.ES384,
                Domain.Interfaces.SignatureAlgorithm.ES512 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.ES512,
                Domain.Interfaces.SignatureAlgorithm.PS256 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.PS256,
                Domain.Interfaces.SignatureAlgorithm.PS384 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.PS384,
                Domain.Interfaces.SignatureAlgorithm.PS512 => Azure.Security.KeyVault.Keys.Cryptography.SignatureAlgorithm.PS512,
                _ => throw new NotSupportedException($"Signature algorithm {algorithm} not supported")
            };

            var verifyResult = await cryptoClient.VerifyDataAsync(
                signAlgorithm,
                data,
                signature,
                cancellationToken);

            _logger.LogDebug("Verified signature with key {KeyName} using {Algorithm}: {IsValid}",
                keyName, algorithm, verifyResult.IsValid);

            return verifyResult.IsValid;
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
            var cryptoClient = await GetCryptographyClientAsync(keyName, cancellationToken);

            var encryptResult = await cryptoClient.EncryptAsync(
                EncryptionAlgorithm.RsaOaep256,
                plaintext,
                cancellationToken);

            _logger.LogDebug("Encrypted data with key {KeyName}", keyName);

            return encryptResult.Ciphertext;
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
            var cryptoClient = await GetCryptographyClientAsync(keyName, cancellationToken);

            var decryptResult = await cryptoClient.DecryptAsync(
                EncryptionAlgorithm.RsaOaep256,
                ciphertext,
                cancellationToken);

            _logger.LogDebug("Decrypted data with key {KeyName}", keyName);

            return decryptResult.Plaintext;
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
            var cryptoClient = await GetCryptographyClientAsync(wrappingKeyName, cancellationToken);

            var wrapResult = await cryptoClient.WrapKeyAsync(
                KeyWrapAlgorithm.RsaOaep256,
                keyToWrap,
                cancellationToken);

            _logger.LogInformation("Wrapped key with {WrappingKeyName}", wrappingKeyName);

            return wrapResult.EncryptedKey;
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
            var cryptoClient = await GetCryptographyClientAsync(wrappingKeyName, cancellationToken);

            var unwrapResult = await cryptoClient.UnwrapKeyAsync(
                KeyWrapAlgorithm.RsaOaep256,
                wrappedKey,
                cancellationToken);

            _logger.LogInformation("Unwrapped key with {WrappingKeyName}", wrappingKeyName);

            return unwrapResult.Key;
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
            var deleteOperation = await _keyClient.StartDeleteKeyAsync(
                keyName,
                cancellationToken);

            await deleteOperation.WaitForCompletionAsync(cancellationToken);

            // Optionally purge the key immediately (requires purge permission)
            if (_configuration.GetValue<bool>("AzureKeyVault:EnablePurge"))
            {
                await _keyClient.PurgeDeletedKeyAsync(keyName, cancellationToken);
            }

            _cryptoClients.Remove(keyName);

            _logger.LogWarning("Deleted key {KeyName}", keyName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete key {KeyName}", keyName);
            return false;
        }
    }

    public async Task<string> RotateKeyAsync(
        string keyName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current key metadata
            var currentKey = await _keyClient.GetKeyAsync(keyName, cancellationToken: cancellationToken);

            // Rotate the key (creates new version)
            var rotatedKey = await _keyClient.RotateKeyAsync(keyName, cancellationToken);

            // Clear cached crypto client for this key
            _cryptoClients.Remove(keyName);

            _logger.LogInformation("Rotated key {KeyName} from version {OldVersion} to {NewVersion}",
                keyName,
                currentKey.Value.Properties.Version,
                rotatedKey.Value.Properties.Version);

            return rotatedKey.Value.Id.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate key {KeyName}", keyName);
            throw;
        }
    }

    public async Task<byte[]> GetPublicKeyAsync(
        string keyName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = await _keyClient.GetKeyAsync(keyName, cancellationToken: cancellationToken);

            // Export public key based on key type
            if (key.Value.KeyType == KeyType.Rsa)
            {
                using var rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters
                {
                    Modulus = key.Value.Key.N,
                    Exponent = key.Value.Key.E
                });
                return rsa.ExportSubjectPublicKeyInfo();
            }
            else if (key.Value.KeyType == KeyType.Ec)
            {
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportParameters(new ECParameters
                {
                    Curve = key.Value.Key.CurveName?.ToString() switch
                    {
                        "P-256" => ECCurve.NamedCurves.nistP256,
                        "P-384" => ECCurve.NamedCurves.nistP384,
                        "P-521" => ECCurve.NamedCurves.nistP521,
                        _ => ECCurve.NamedCurves.nistP256
                    },
                    Q = new ECPoint
                    {
                        X = key.Value.Key.X,
                        Y = key.Value.Key.Y
                    }
                });
                return ecdsa.ExportSubjectPublicKeyInfo();
            }

            throw new NotSupportedException($"Key type {key.Value.KeyType} not supported for public key export");
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
            // Create certificate policy for CSR generation
            var policy = new CertificatePolicy("Unknown", subjectName.Name)
            {
                ValidityInMonths = 12,
                Enabled = true
            };

            // Start the certificate creation (this generates CSR)
            var operation = await _certificateClient.StartCreateCertificateAsync(
                keyName,
                policy,
                cancellationToken: cancellationToken);

            // The CSR is in the pending operation
            // Note: Azure Key Vault doesn't directly expose CSR as raw bytes in the same way
            // This is a simplified implementation
            _logger.LogInformation("Started certificate creation for key {KeyName}", keyName);

            // For actual CSR, you would need to implement proper X.509 CSR generation
            // or use the certificate operation's properties
            return Encoding.UTF8.GetBytes($"CSR_FOR_{keyName}");
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
            // Export certificate as PFX (with empty password for Key Vault)
            var certificateBytes = certificate.Export(X509ContentType.Pfx);

            var importOptions = new ImportCertificateOptions(
                keyName,
                certificateBytes)
            {
                Enabled = true,
                Policy = new CertificatePolicy("Unknown", certificate.Subject)
            };

            await _certificateClient.ImportCertificateAsync(
                importOptions,
                cancellationToken);

            _logger.LogInformation("Imported certificate for key {KeyName}", keyName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import certificate for key {KeyName}", keyName);
            return false;
        }
    }

    public async Task<HsmKeyMetadata> GetKeyMetadataAsync(
        string keyName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = await _keyClient.GetKeyAsync(keyName, cancellationToken: cancellationToken);

            var metadata = new HsmKeyMetadata
            {
                KeyId = key.Value.Id.ToString(),
                KeyName = key.Value.Name,
                Algorithm = DetermineKeyAlgorithm(key.Value),
                CreatedAt = key.Value.Properties.CreatedOn ?? DateTimeOffset.MinValue,
                ExpiresAt = key.Value.Properties.ExpiresOn,
                Version = key.Value.Properties.Version,
                Enabled = key.Value.Properties.Enabled ?? false,
                Tags = new Dictionary<string, string>(key.Value.Properties.Tags ?? new Dictionary<string, string>()),
                AllowedOperations = key.Value.Key.KeyOps?.Select(op => op.ToString()).ToList() ?? new List<string>()
            };

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metadata for key {KeyName}", keyName);
            throw;
        }
    }

    public async Task<IEnumerable<string>> ListKeysAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var keys = new List<string>();

            await foreach (var key in _keyClient.GetPropertiesOfKeysAsync(cancellationToken))
            {
                if (key.Enabled == true)
                {
                    keys.Add(key.Name);
                }
            }

            _logger.LogDebug("Listed {Count} keys from HSM", keys.Count);

            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list keys from HSM");
            throw;
        }
    }

    public async Task<HsmHealthStatus> GetHealthStatusAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = new HsmHealthStatus
            {
                CheckedAt = DateTimeOffset.UtcNow,
                Details = new Dictionary<string, object>()
            };

            // Try to list keys as a health check
            var keyCount = 0;
            await foreach (var key in _keyClient.GetPropertiesOfKeysAsync(cancellationToken))
            {
                keyCount++;
                if (keyCount >= 1)
                {
                    break; // Just check if we can access at least one
                }
            }

            status.IsHealthy = true;
            status.Status = "Healthy";
            status.Details["accessible"] = true;
            status.Details["keyVaultUri"] = _configuration["AzureKeyVault:Uri"] ?? "Not configured";
            status.Details["timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            _logger.LogDebug("HSM health check passed");

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HSM health check failed");

            return new HsmHealthStatus
            {
                IsHealthy = false,
                Status = "Unhealthy",
                CheckedAt = DateTimeOffset.UtcNow,
                Details = new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["accessible"] = false,
                    ["timestamp"] = DateTimeOffset.UtcNow.ToString("O")
                }
            };
        }
    }

    private async Task<CryptographyClient> GetCryptographyClientAsync(
        string keyName,
        CancellationToken cancellationToken)
    {
        if (!_cryptoClients.TryGetValue(keyName, out var client))
        {
            var key = await _keyClient.GetKeyAsync(keyName, cancellationToken: cancellationToken);
            client = new CryptographyClient(key.Value.Id, new DefaultAzureCredential());
            _cryptoClients[keyName] = client;
        }

        return client;
    }

    private static KeyAlgorithm DetermineKeyAlgorithm(KeyVaultKey key)
    {
        if (key.KeyType == KeyType.Rsa)
        {
            var keySize = key.Key.N?.Length * 8 ?? 0;
            if (keySize <= 2048)
            {
                return KeyAlgorithm.RSA2048;
            }
            if (keySize <= 3072)
            {
                return KeyAlgorithm.RSA3072;
            }
            if (keySize <= 4096)
            {
                return KeyAlgorithm.RSA4096;
            }
            return KeyAlgorithm.RSA4096;
        }
        else if (key.KeyType == KeyType.Ec)
        {
            var curveName = key.Key.CurveName?.ToString();
            if (curveName == "P-256")
            {
                return KeyAlgorithm.ECC_P256;
            }
            if (curveName == "P-384")
            {
                return KeyAlgorithm.ECC_P384;
            }
            if (curveName == "P-521")
            {
                return KeyAlgorithm.ECC_P521;
            }
            return KeyAlgorithm.ECC_P256;
        }
        else if (key.KeyType == KeyType.Oct)
        {
            return KeyAlgorithm.AES256;
        }

        return KeyAlgorithm.RSA2048; // Default fallback
    }
}
