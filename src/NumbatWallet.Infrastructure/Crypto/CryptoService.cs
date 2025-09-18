using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Infrastructure.Crypto.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Infrastructure.Crypto;

/// <summary>
/// Provides envelope encryption services for multi-tenant data protection
/// </summary>
public class CryptoService : ICryptoService
{
    private readonly IKeyWrapProvider _wrapProvider;
    private readonly SecretClient _secretClient;
    private readonly ITenantService _tenantService;
    private readonly IMemoryCache _dekCache;
    private readonly ILogger<CryptoService> _logger;
    private readonly TimeSpan _dekCacheDuration = TimeSpan.FromMinutes(5);

    public CryptoService(
        IKeyWrapProvider wrapProvider,
        ITenantService tenantService,
        IMemoryCache memoryCache,
        IConfiguration configuration,
        ILogger<CryptoService> logger)
    {
        _wrapProvider = wrapProvider;
        _tenantService = tenantService;
        _dekCache = memoryCache;
        _logger = logger;

        var kvUri = configuration["KeyVault:Uri"]
            ?? throw new InvalidOperationException("KeyVault:Uri configuration is required");
        _secretClient = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
    }

    public async Task<string> EncryptAsync(string plaintext, DataClassification classification)
    {
        // No encryption for public/official data
        if (classification < DataClassification.OfficialSensitive)
        {
            return plaintext;
        }

        if (string.IsNullOrEmpty(plaintext))
        {
            return plaintext;
        }

        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var encryptedBytes = await EncryptBytesAsync(plainBytes, classification);
        return Convert.ToBase64String(encryptedBytes);
    }

    public async Task<string> DecryptAsync(string ciphertext, DataClassification classification)
    {
        // No decryption needed for public/official data
        if (classification < DataClassification.OfficialSensitive)
        {
            return ciphertext;
        }

        if (string.IsNullOrEmpty(ciphertext))
        {
            return ciphertext;
        }

        var cipherBytes = Convert.FromBase64String(ciphertext);
        var plainBytes = await DecryptBytesAsync(cipherBytes, classification);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public async Task<byte[]> EncryptBytesAsync(byte[] plainBytes, DataClassification classification)
    {
        // No encryption for public/official data
        if (classification < DataClassification.OfficialSensitive)
        {
            return plainBytes;
        }

        var dek = await GetOrCreateDekAsync();

        // Generate nonce (12 bytes for AES-GCM)
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        // Generate tag (16 bytes for AES-GCM)
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        // Encrypt the data
        var cipherBytes = new byte[plainBytes.Length];
        using (var aesGcm = new AesGcm(dek, AesGcm.TagByteSizes.MaxSize))
        {
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        // Combine: version(1) + nonce(12) + tag(16) + ciphertext
        var result = new byte[1 + nonce.Length + tag.Length + cipherBytes.Length];
        result[0] = 1; // Version
        Buffer.BlockCopy(nonce, 0, result, 1, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, 1 + nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, 1 + nonce.Length + tag.Length, cipherBytes.Length);

        // Clear sensitive data from memory
        CryptographicOperations.ZeroMemory(dek);

        return result;
    }

    public async Task<byte[]> DecryptBytesAsync(byte[] cipherBytes, DataClassification classification)
    {
        // No decryption needed for public/official data
        if (classification < DataClassification.OfficialSensitive)
        {
            return cipherBytes;
        }

        if (cipherBytes.Length < 29) // Minimum size: version(1) + nonce(12) + tag(16)
        {
            throw new ArgumentException("Invalid encrypted data format");
        }

        var version = cipherBytes[0];
        if (version != 1)
        {
            throw new NotSupportedException($"Unsupported encryption version: {version}");
        }

        // Extract components
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        Buffer.BlockCopy(cipherBytes, 1, nonce, 0, nonce.Length);
        Buffer.BlockCopy(cipherBytes, 1 + nonce.Length, tag, 0, tag.Length);

        var cipherDataLength = cipherBytes.Length - 1 - nonce.Length - tag.Length;
        var cipherData = new byte[cipherDataLength];
        Buffer.BlockCopy(cipherBytes, 1 + nonce.Length + tag.Length, cipherData, 0, cipherDataLength);

        // Get DEK and decrypt
        var dek = await GetOrCreateDekAsync();
        var plainBytes = new byte[cipherDataLength];

        using (var aesGcm = new AesGcm(dek, AesGcm.TagByteSizes.MaxSize))
        {
            aesGcm.Decrypt(nonce, cipherData, tag, plainBytes);
        }

        // Clear sensitive data from memory
        CryptographicOperations.ZeroMemory(dek);

        return plainBytes;
    }

    public async Task RotateDekAsync(string tenantId)
    {
        var cacheKey = GetDekCacheKey(tenantId);
        _dekCache.Remove(cacheKey);

        // Generate new DEK
        var newDek = GenerateDek();

        // Get current KEK ID
        var kekId = await GetCurrentKekIdAsync(tenantId);

        // Wrap new DEK with KEK
        var wrappedDek = await _wrapProvider.WrapAsync(newDek, kekId, tenantId);

        // Store wrapped DEK in Key Vault
        var secretName = GetDekSecretName(tenantId);
        var secretValue = Convert.ToBase64String(wrappedDek);
        await _secretClient.SetSecretAsync(new KeyVaultSecret(secretName, secretValue)
        {
            Properties =
            {
                ExpiresOn = DateTimeOffset.UtcNow.AddYears(1),
                Tags =
                {
                    ["TenantId"] = tenantId,
                    ["Purpose"] = "DEK",
                    ["Version"] = DateTimeOffset.UtcNow.Ticks.ToString()
                }
            }
        });

        _logger.LogInformation("Rotated DEK for tenant {TenantId}", tenantId);

        // Clear sensitive data
        CryptographicOperations.ZeroMemory(newDek);
    }

    public async Task<int> GetCurrentDekVersionAsync(string tenantId)
    {
        try
        {
            var secretName = GetDekSecretName(tenantId);
            var secret = await _secretClient.GetSecretAsync(secretName);

            if (secret.Value.Properties.Tags.TryGetValue("Version", out var version))
            {
                return int.Parse(version);
            }

            return 1;
        }
        catch
        {
            return 1;
        }
    }

    private async Task<byte[]> GetOrCreateDekAsync()
    {
        var tenantId = _tenantService.TenantId.ToString();
        var cacheKey = GetDekCacheKey(tenantId);

        // Check cache first
        if (_dekCache.TryGetValue<byte[]>(cacheKey, out var cachedDek) && cachedDek != null)
        {
            return cachedDek;
        }

        // Try to get existing wrapped DEK from Key Vault
        var secretName = GetDekSecretName(tenantId);
        byte[] dek;

        try
        {
            var secret = await _secretClient.GetSecretAsync(secretName);
            var wrappedDek = Convert.FromBase64String(secret.Value.Value);

            // Get current KEK ID
            var kekId = await GetCurrentKekIdAsync(tenantId);

            // Unwrap DEK
            dek = await _wrapProvider.UnwrapAsync(wrappedDek, kekId, tenantId);
        }
        catch
        {
            // No existing DEK, create new one
            dek = await CreateNewDekAsync(tenantId);
        }

        // Cache DEK (with sliding expiration)
        _dekCache.Set(cacheKey, dek, new MemoryCacheEntryOptions
        {
            SlidingExpiration = _dekCacheDuration,
            Priority = CacheItemPriority.High,
            PostEvictionCallbacks =
            {
                new PostEvictionCallbackRegistration
                {
                    EvictionCallback = (key, value, reason, state) =>
                    {
                        if (value is byte[] keyData)
                        {
                            CryptographicOperations.ZeroMemory(keyData);
                        }
                    }
                }
            }
        });

        return dek;
    }

    private async Task<byte[]> CreateNewDekAsync(string tenantId)
    {
        // Generate new DEK
        var dek = GenerateDek();

        // Create or get KEK
        var kekId = await GetOrCreateKekAsync(tenantId);

        // Wrap DEK with KEK
        var wrappedDek = await _wrapProvider.WrapAsync(dek, kekId, tenantId);

        // Store wrapped DEK in Key Vault
        var secretName = GetDekSecretName(tenantId);
        var secretValue = Convert.ToBase64String(wrappedDek);
        await _secretClient.SetSecretAsync(new KeyVaultSecret(secretName, secretValue)
        {
            Properties =
            {
                ExpiresOn = DateTimeOffset.UtcNow.AddYears(1),
                Tags =
                {
                    ["TenantId"] = tenantId,
                    ["Purpose"] = "DEK",
                    ["Version"] = "1"
                }
            }
        });

        _logger.LogInformation("Created new DEK for tenant {TenantId}", tenantId);

        return dek;
    }

    private async Task<string> GetOrCreateKekAsync(string tenantId)
    {
        try
        {
            var kekId = await GetCurrentKekIdAsync(tenantId);
            return kekId;
        }
        catch
        {
            // No existing KEK, create new one
            var kekId = await _wrapProvider.CreateKekAsync(tenantId, new KekProperties());

            // Store KEK ID reference
            await _secretClient.SetSecretAsync(new KeyVaultSecret(
                GetKekReferenceSecretName(tenantId),
                kekId)
            {
                Properties =
                {
                    Tags =
                    {
                        ["TenantId"] = tenantId,
                        ["Purpose"] = "KEK-Reference"
                    }
                }
            });

            return kekId;
        }
    }

    private async Task<string> GetCurrentKekIdAsync(string tenantId)
    {
        var secretName = GetKekReferenceSecretName(tenantId);
        var secret = await _secretClient.GetSecretAsync(secretName);
        return secret.Value.Value;
    }

    private static byte[] GenerateDek()
    {
        var key = new byte[32]; // 256 bits for AES-256
        RandomNumberGenerator.Fill(key);
        return key;
    }

    private static string GetDekCacheKey(string tenantId) => $"dek:{tenantId}";
    private static string GetDekSecretName(string tenantId) => $"dek-{tenantId}".ToLowerInvariant();
    private static string GetKekReferenceSecretName(string tenantId) => $"kek-ref-{tenantId}".ToLowerInvariant();
}