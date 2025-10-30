using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Infrastructure.Services.Providers;

/// <summary>
/// Software-based HSM provider for development and testing
/// Uses file system for key storage with AES-256 encryption
/// </summary>
public class SoftwareHsmProvider : IHsmProvider
{
    private readonly ILogger<SoftwareHsmProvider> _logger;
    private readonly ConcurrentDictionary<string, SoftwareKey> _keys;
    private readonly string _keyStorePath;
    private readonly byte[] _masterKey;
    private readonly object _persistLock = new();

    public string ProviderType => "Software";
    public bool SupportsHardwareBackedKeys => false;
    public FipsComplianceLevel ComplianceLevel => FipsComplianceLevel.None;

    public SoftwareHsmProvider(
        IConfiguration configuration,
        ILogger<SoftwareHsmProvider> logger)
    {
        _logger = logger;
        _keys = new ConcurrentDictionary<string, SoftwareKey>();

        // Setup key store path
        _keyStorePath = configuration["SoftwareHsm:KeyStorePath"]
            ?? Path.Combine(Path.GetTempPath(), "numbatwallet", "keys");

        Directory.CreateDirectory(_keyStorePath);

        // Derive master key from configuration (development only!)
        var masterKeyPassword = configuration["SoftwareHsm:MasterKeyPassword"]
            ?? "DevOnly-ChangeInProduction!";
        _masterKey = DeriveKey(masterKeyPassword, "NumbatWallet-DevHSM");

        // Load existing keys
        LoadKeys();

        _logger.LogInformation("Software HSM Provider initialized with key store at {Path}", _keyStorePath);
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        // Software provider is always available
        return Task.FromResult(true);
    }

    public async Task<HsmKey> GenerateKeyAsync(
        KeyGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating {Type} key: {Name}", request.KeyType, request.KeyName);

        var keyId = $"software-{Guid.NewGuid():N}";
        var key = new SoftwareKey
        {
            Id = keyId,
            Name = request.KeyName,
            Type = request.KeyType,
            KeySize = request.KeySize,
            Usage = request.Usage,
            CreatedOn = DateTime.UtcNow,
            ExpiresOn = request.ExpiresOn,
            Enabled = true,
            Tags = request.Tags
        };

        // Generate key material based on type
        switch (request.KeyType)
        {
            case KeyType.RSA:
                using (var rsa = RSA.Create(request.KeySize))
                {
                    key.KeyMaterial = rsa.ExportRSAPrivateKey();
                    key.PublicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
                }
                break;

            case KeyType.EC:
                using (var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256))
                {
                    key.KeyMaterial = ec.ExportECPrivateKey();
                    key.PublicKey = Convert.ToBase64String(ec.ExportSubjectPublicKeyInfo());
                }
                break;

            case KeyType.AES:
                key.KeyMaterial = RandomNumberGenerator.GetBytes(request.KeySize / 8);
                break;

            case KeyType.HMAC:
                key.KeyMaterial = RandomNumberGenerator.GetBytes(request.KeySize / 8);
                break;

            default:
                throw new NotSupportedException($"Key type {request.KeyType} not supported");
        }

        // Store key
        _keys[keyId] = key;
        await PersistKeysAsync();

        return new HsmKey
        {
            Id = key.Id,
            Name = key.Name,
            Type = key.Type,
            KeySize = key.KeySize,
            Usage = key.Usage,
            IsHardwareBacked = false,
            CreatedOn = key.CreatedOn,
            ExpiresOn = key.ExpiresOn,
            Enabled = key.Enabled,
            Version = "1.0",
            Tags = key.Tags,
            PublicKey = key.PublicKey
        };
    }

    public async Task<byte[]> SignAsync(
        string keyId,
        byte[] data,
        SigningAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        if (!_keys.TryGetValue(keyId, out var key))
        {
            throw new KeyNotFoundException($"Key {keyId} not found");
        }

        key.LastUsedOn = DateTime.UtcNow;

        byte[] signature;
        switch (key.Type)
        {
            case KeyType.RSA:
                using (var rsa = RSA.Create())
                {
                    rsa.ImportRSAPrivateKey(key.KeyMaterial, out _);
                    signature = algorithm switch
                    {
                        SigningAlgorithm.RS256 => rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                        SigningAlgorithm.RS384 => rsa.SignData(data, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1),
                        SigningAlgorithm.RS512 => rsa.SignData(data, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1),
                        SigningAlgorithm.PS256 => rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
                        SigningAlgorithm.PS384 => rsa.SignData(data, HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
                        SigningAlgorithm.PS512 => rsa.SignData(data, HashAlgorithmName.SHA512, RSASignaturePadding.Pss),
                        _ => throw new NotSupportedException($"Algorithm {algorithm} not supported for RSA")
                    };
                }
                break;

            case KeyType.EC:
                using (var ec = ECDsa.Create())
                {
                    ec.ImportECPrivateKey(key.KeyMaterial, out _);
                    signature = algorithm switch
                    {
                        SigningAlgorithm.ES256 => ec.SignData(data, HashAlgorithmName.SHA256),
                        SigningAlgorithm.ES384 => ec.SignData(data, HashAlgorithmName.SHA384),
                        SigningAlgorithm.ES512 => ec.SignData(data, HashAlgorithmName.SHA512),
                        _ => throw new NotSupportedException($"Algorithm {algorithm} not supported for EC")
                    };
                }
                break;

            default:
                throw new NotSupportedException($"Key type {key.Type} cannot be used for signing");
        }

        _logger.LogDebug("Signed data with key {KeyId} using {Algorithm}", keyId, algorithm);
        return signature;
    }

    public async Task<bool> VerifyAsync(
        string keyId,
        byte[] data,
        byte[] signature,
        SigningAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        if (!_keys.TryGetValue(keyId, out var key))
        {
            throw new KeyNotFoundException($"Key {keyId} not found");
        }

        bool isValid;
        switch (key.Type)
        {
            case KeyType.RSA:
                using (var rsa = RSA.Create())
                {
                    rsa.ImportRSAPrivateKey(key.KeyMaterial, out _);
                    isValid = algorithm switch
                    {
                        SigningAlgorithm.RS256 => rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                        SigningAlgorithm.RS384 => rsa.VerifyData(data, signature, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1),
                        SigningAlgorithm.RS512 => rsa.VerifyData(data, signature, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1),
                        SigningAlgorithm.PS256 => rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
                        SigningAlgorithm.PS384 => rsa.VerifyData(data, signature, HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
                        SigningAlgorithm.PS512 => rsa.VerifyData(data, signature, HashAlgorithmName.SHA512, RSASignaturePadding.Pss),
                        _ => throw new NotSupportedException($"Algorithm {algorithm} not supported for RSA")
                    };
                }
                break;

            case KeyType.EC:
                using (var ec = ECDsa.Create())
                {
                    ec.ImportECPrivateKey(key.KeyMaterial, out _);
                    isValid = algorithm switch
                    {
                        SigningAlgorithm.ES256 => ec.VerifyData(data, signature, HashAlgorithmName.SHA256),
                        SigningAlgorithm.ES384 => ec.VerifyData(data, signature, HashAlgorithmName.SHA384),
                        SigningAlgorithm.ES512 => ec.VerifyData(data, signature, HashAlgorithmName.SHA512),
                        _ => throw new NotSupportedException($"Algorithm {algorithm} not supported for EC")
                    };
                }
                break;

            default:
                throw new NotSupportedException($"Key type {key.Type} cannot be used for verification");
        }

        return isValid;
    }

    public async Task<byte[]> EncryptAsync(
        string keyId,
        byte[] plaintext,
        EncryptionAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        if (!_keys.TryGetValue(keyId, out var key))
        {
            throw new KeyNotFoundException($"Key {keyId} not found");
        }

        key.LastUsedOn = DateTime.UtcNow;

        byte[] ciphertext;
        switch (key.Type)
        {
            case KeyType.RSA:
                using (var rsa = RSA.Create())
                {
                    rsa.ImportRSAPrivateKey(key.KeyMaterial, out _);
                    ciphertext = algorithm switch
                    {
                        EncryptionAlgorithm.RSA_OAEP => rsa.Encrypt(plaintext, RSAEncryptionPadding.OaepSHA1),
                        EncryptionAlgorithm.RSA_OAEP_256 => rsa.Encrypt(plaintext, RSAEncryptionPadding.OaepSHA256),
                        _ => throw new NotSupportedException($"Algorithm {algorithm} not supported for RSA")
                    };
                }
                break;

            case KeyType.AES:
                ciphertext = algorithm switch
                {
                    EncryptionAlgorithm.AES_GCM => EncryptAesGcm(key.KeyMaterial, plaintext),
                    EncryptionAlgorithm.AES_CBC => EncryptAesCbc(key.KeyMaterial, plaintext),
                    _ => throw new NotSupportedException($"Algorithm {algorithm} not supported for AES")
                };
                break;

            default:
                throw new NotSupportedException($"Key type {key.Type} cannot be used for encryption");
        }

        return ciphertext;
    }

    public async Task<byte[]> DecryptAsync(
        string keyId,
        byte[] ciphertext,
        EncryptionAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        if (!_keys.TryGetValue(keyId, out var key))
        {
            throw new KeyNotFoundException($"Key {keyId} not found");
        }

        key.LastUsedOn = DateTime.UtcNow;

        byte[] plaintext;
        switch (key.Type)
        {
            case KeyType.RSA:
                using (var rsa = RSA.Create())
                {
                    rsa.ImportRSAPrivateKey(key.KeyMaterial, out _);
                    plaintext = algorithm switch
                    {
                        EncryptionAlgorithm.RSA_OAEP => rsa.Decrypt(ciphertext, RSAEncryptionPadding.OaepSHA1),
                        EncryptionAlgorithm.RSA_OAEP_256 => rsa.Decrypt(ciphertext, RSAEncryptionPadding.OaepSHA256),
                        _ => throw new NotSupportedException($"Algorithm {algorithm} not supported for RSA")
                    };
                }
                break;

            case KeyType.AES:
                plaintext = algorithm switch
                {
                    EncryptionAlgorithm.AES_GCM => DecryptAesGcm(key.KeyMaterial, ciphertext),
                    EncryptionAlgorithm.AES_CBC => DecryptAesCbc(key.KeyMaterial, ciphertext),
                    _ => throw new NotSupportedException($"Algorithm {algorithm} not supported for AES")
                };
                break;

            default:
                throw new NotSupportedException($"Key type {key.Type} cannot be used for decryption");
        }

        return plaintext;
    }

    public async Task<byte[]> WrapKeyAsync(
        string wrappingKeyId,
        byte[] keyToWrap,
        KeyWrapAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        if (!_keys.TryGetValue(wrappingKeyId, out var key))
        {
            throw new KeyNotFoundException($"Wrapping key {wrappingKeyId} not found");
        }

        // For simplicity, encrypt the key material
        return await EncryptAsync(
            wrappingKeyId,
            keyToWrap,
            algorithm == KeyWrapAlgorithm.AES_KW ? EncryptionAlgorithm.AES_CBC : EncryptionAlgorithm.RSA_OAEP_256,
            cancellationToken);
    }

    public async Task<byte[]> UnwrapKeyAsync(
        string unwrappingKeyId,
        byte[] wrappedKey,
        KeyWrapAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        if (!_keys.TryGetValue(unwrappingKeyId, out var key))
        {
            throw new KeyNotFoundException($"Unwrapping key {unwrappingKeyId} not found");
        }

        // For simplicity, decrypt the key material
        return await DecryptAsync(
            unwrappingKeyId,
            wrappedKey,
            algorithm == KeyWrapAlgorithm.AES_KW ? EncryptionAlgorithm.AES_CBC : EncryptionAlgorithm.RSA_OAEP_256,
            cancellationToken);
    }

    public async Task<KeyBackupData> BackupKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        if (!_keys.TryGetValue(keyId, out var key))
        {
            throw new KeyNotFoundException($"Key {keyId} not found");
        }

        var backup = new KeyBackup
        {
            Key = key,
            BackupDate = DateTime.UtcNow,
            Version = "1.0"
        };

        var json = JsonSerializer.Serialize(backup);
        var encryptedBackup = EncryptWithMasterKey(Encoding.UTF8.GetBytes(json));

        return new KeyBackupData
        {
            KeyId = keyId,
            BackupBlob = encryptedBackup,
            BackupVersion = "1.0",
            BackupDate = backup.BackupDate,
            SourceProvider = ProviderType,
            Metadata = new Dictionary<string, string>
            {
                ["KeyName"] = key.Name,
                ["KeyType"] = key.Type.ToString(),
                ["CreatedOn"] = key.CreatedOn.ToString("O")
            }
        };
    }

    public async Task<string> RestoreKeyAsync(
        KeyBackupData backup,
        CancellationToken cancellationToken = default)
    {
        var decryptedBackup = DecryptWithMasterKey(backup.BackupBlob);
        var json = Encoding.UTF8.GetString(decryptedBackup);
        var backupData = JsonSerializer.Deserialize<KeyBackup>(json)
            ?? throw new InvalidOperationException("Invalid backup data");

        var newKeyId = $"software-{Guid.NewGuid():N}";
        backupData.Key.Id = newKeyId;
        backupData.Key.Name = $"{backupData.Key.Name}-restored-{DateTime.UtcNow:yyyyMMddHHmmss}";

        _keys[newKeyId] = backupData.Key;
        await PersistKeysAsync();

        _logger.LogInformation("Restored key {OriginalId} as {NewId}", backup.KeyId, newKeyId);
        return newKeyId;
    }

    public async Task<bool> DeleteKeyAsync(
        string keyId,
        bool permanentDelete = false,
        CancellationToken cancellationToken = default)
    {
        if (_keys.TryRemove(keyId, out var key))
        {
            if (!permanentDelete)
            {
                // Soft delete - mark as deleted but keep in storage
                key.Enabled = false;
                key.DeletedOn = DateTime.UtcNow;
                _keys[keyId] = key;
            }

            await PersistKeysAsync();
            _logger.LogInformation("Deleted key {KeyId} (permanent: {Permanent})", keyId, permanentDelete);
            return true;
        }

        return false;
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
            // Backup key from source
            var backup = await BackupKeyAsync(keyId, cancellationToken);

            // Restore to target
            var newKeyId = await targetProvider.RestoreKeyAsync(backup, cancellationToken);

            // Verify if requested
            if (options.VerifyAfterMigration)
            {
                var testData = Encoding.UTF8.GetBytes("Migration verification test");
                var signature = await SignAsync(keyId, testData, SigningAlgorithm.RS256, cancellationToken);
                var isValid = await targetProvider.VerifyAsync(newKeyId, testData, signature, SigningAlgorithm.RS256, cancellationToken);

                if (!isValid)
                {
                    throw new InvalidOperationException("Migration verification failed");
                }
            }

            // Delete source if requested
            if (options.DeleteSourceAfterMigration)
            {
                await DeleteKeyAsync(keyId, true, cancellationToken);
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
        if (!_keys.TryGetValue(keyId, out var key))
        {
            throw new KeyNotFoundException($"Key {keyId} not found");
        }

        return new HsmKey
        {
            Id = key.Id,
            Name = key.Name,
            Type = key.Type,
            KeySize = key.KeySize,
            Usage = key.Usage,
            IsHardwareBacked = false,
            CreatedOn = key.CreatedOn,
            ExpiresOn = key.ExpiresOn,
            LastUsedOn = key.LastUsedOn,
            Enabled = key.Enabled,
            Version = "1.0",
            Tags = key.Tags,
            PublicKey = key.PublicKey
        };
    }

    public async Task<IEnumerable<HsmKey>> ListKeysAsync(
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        var keys = _keys.Values
            .Where(k => k.Enabled && (prefix == null || k.Name.StartsWith(prefix, StringComparison.Ordinal)))
            .Select(k => new HsmKey
            {
                Id = k.Id,
                Name = k.Name,
                Type = k.Type,
                KeySize = k.KeySize,
                Usage = k.Usage,
                IsHardwareBacked = false,
                CreatedOn = k.CreatedOn,
                ExpiresOn = k.ExpiresOn,
                LastUsedOn = k.LastUsedOn,
                Enabled = k.Enabled,
                Version = "1.0",
                Tags = k.Tags,
                PublicKey = k.PublicKey
            })
            .ToList();

        return keys;
    }

    public HsmProviderConfiguration GetConfiguration()
    {
        return new HsmProviderConfiguration
        {
            ProviderType = ProviderType,
            Settings = new Dictionary<string, string>
            {
                ["KeyStorePath"] = _keyStorePath,
                ["Encrypted"] = "true",
                ["ComplianceLevel"] = ComplianceLevel.ToString()
            },
            CachingEnabled = false
        };
    }

    public virtual async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Test key generation
            var testKey = await GenerateKeyAsync(new KeyGenerationRequest
            {
                KeyName = $"health-check-{Guid.NewGuid():N}",
                KeyType = KeyType.RSA,
                KeySize = 2048,
                Usage = KeyUsage.Sign | KeyUsage.Verify
            }, cancellationToken);

            // Test signing
            var testData = Encoding.UTF8.GetBytes("Health check test data");
            var signature = await SignAsync(testKey.Id, testData, SigningAlgorithm.RS256, cancellationToken);
            var isValid = await VerifyAsync(testKey.Id, testData, signature, SigningAlgorithm.RS256, cancellationToken);

            // Clean up test key
            await DeleteKeyAsync(testKey.Id, true, cancellationToken);

            return new HealthCheckResult
            {
                IsHealthy = isValid,
                Status = "Healthy",
                ResponseTime = DateTime.UtcNow - startTime,
                Metrics = new Dictionary<string, object>
                {
                    ["TotalKeys"] = _keys.Count,
                    ["ActiveKeys"] = _keys.Count(k => k.Value.Enabled),
                    ["StoragePath"] = _keyStorePath
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

    private void LoadKeys()
    {
        var keyFile = Path.Combine(_keyStorePath, "keys.enc");
        if (!File.Exists(keyFile))
        {
            return;
        }

        try
        {
            var encryptedData = File.ReadAllBytes(keyFile);
            var decryptedData = DecryptWithMasterKey(encryptedData);
            var json = Encoding.UTF8.GetString(decryptedData);
            var keys = JsonSerializer.Deserialize<Dictionary<string, SoftwareKey>>(json);

            if (keys != null)
            {
                foreach (var kvp in keys)
                {
                    _keys[kvp.Key] = kvp.Value;
                }
            }

            _logger.LogInformation("Loaded {Count} keys from storage", _keys.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load keys from storage");
        }
    }

    private async Task PersistKeysAsync()
    {
        await Task.Run(() =>
        {
            lock (_persistLock)
            {
                var keyFile = Path.Combine(_keyStorePath, "keys.enc");
                var json = JsonSerializer.Serialize(_keys);
                var encryptedData = EncryptWithMasterKey(Encoding.UTF8.GetBytes(json));
                File.WriteAllBytes(keyFile, encryptedData);
            }
        });
    }

    private byte[] DeriveKey(string password, string salt)
    {
        using var deriveBytes = new Rfc2898DeriveBytes(
            password,
            Encoding.UTF8.GetBytes(salt),
            100000,
            HashAlgorithmName.SHA256);
        return deriveBytes.GetBytes(32);
    }

    private byte[] EncryptWithMasterKey(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = _masterKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
        }
        return ms.ToArray();
    }

    private byte[] DecryptWithMasterKey(byte[] encryptedData)
    {
        using var aes = Aes.Create();
        aes.Key = _masterKey;

        var iv = new byte[16];
        Array.Copy(encryptedData, 0, iv, 0, 16);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(encryptedData, 16, encryptedData.Length - 16);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var result = new MemoryStream();
        cs.CopyTo(result);
        return result.ToArray();
    }

    private byte[] EncryptAesGcm(byte[] key, byte[] plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using var aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

        return result;
    }

    private byte[] DecryptAesGcm(byte[] key, byte[] encryptedData)
    {
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        var ciphertext = new byte[encryptedData.Length - nonce.Length - tag.Length];

        Buffer.BlockCopy(encryptedData, 0, nonce, 0, nonce.Length);
        Buffer.BlockCopy(encryptedData, nonce.Length, tag, 0, tag.Length);
        Buffer.BlockCopy(encryptedData, nonce.Length + tag.Length, ciphertext, 0, ciphertext.Length);

        var plaintext = new byte[ciphertext.Length];
        using var aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    private byte[] EncryptAesCbc(byte[] key, byte[] plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plaintext, 0, plaintext.Length);
        }
        return ms.ToArray();
    }

    private byte[] DecryptAesCbc(byte[] key, byte[] encryptedData)
    {
        using var aes = Aes.Create();
        aes.Key = key;

        var iv = new byte[16];
        Array.Copy(encryptedData, 0, iv, 0, 16);
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(encryptedData, 16, encryptedData.Length - 16);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var result = new MemoryStream();
        cs.CopyTo(result);
        return result.ToArray();
    }

    #endregion

    #region Internal Classes

    private class SoftwareKey
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public KeyType Type { get; set; }
        public int KeySize { get; set; }
        public KeyUsage Usage { get; set; }
        public byte[] KeyMaterial { get; set; } = Array.Empty<byte>();
        public string? PublicKey { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ExpiresOn { get; set; }
        public DateTime? LastUsedOn { get; set; }
        public DateTime? DeletedOn { get; set; }
        public bool Enabled { get; set; } = true;
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    private class KeyBackup
    {
        public SoftwareKey Key { get; set; } = new();
        public DateTime BackupDate { get; set; }
        public string Version { get; set; } = string.Empty;
    }

    #endregion
}

public class KeyNotFoundException : Exception
{
    public KeyNotFoundException(string message) : base(message) { }
}
