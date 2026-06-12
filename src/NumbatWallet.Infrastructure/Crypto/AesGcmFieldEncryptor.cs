using System.Security.Cryptography;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Crypto;

/// <summary>
/// AES-256-GCM field encryptor. The 256-bit key is sourced (in priority order) from a Key Vault
/// secret (FieldEncryption:KeyVaultUri + FieldEncryption:KeySecretName) or from configuration
/// (FieldEncryption:Key, base64) for local dev. If no key is configured, encryption is disabled
/// and values pass through unchanged.
///
/// Token format: "FE1:" + base64( nonce(12) | tag(16) | ciphertext ). Decrypt returns any
/// non-token input unchanged so legacy plaintext rows remain readable without a migration.
/// </summary>
public sealed class AesGcmFieldEncryptor : IFieldEncryptor
{
    private const string Prefix = "FE1:";
    private const int NonceSize = 12;  // AES-GCM standard nonce
    private const int TagSize = 16;    // AES-GCM tag
    private readonly byte[]? _key;

    public AesGcmFieldEncryptor(IConfiguration configuration, ILogger<AesGcmFieldEncryptor> logger)
    {
        _key = LoadKey(configuration, logger);
        if (_key is not null && _key.Length != 32)
        {
            throw new InvalidOperationException(
                $"Field encryption key must be 256 bits (32 bytes); got {_key.Length} bytes.");
        }
        IsEnabled = _key is not null;
        logger.LogInformation("Field-level encryption is {State}", IsEnabled ? "ENABLED (AES-256-GCM)" : "disabled");
    }

    public bool IsEnabled { get; }

    public string Encrypt(string plaintext)
    {
        if (!IsEnabled || string.IsNullOrEmpty(plaintext))
        {
            return plaintext;
        }

        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(_key!, TagSize))
        {
            aes.Encrypt(nonce, plainBytes, cipher, tag);
        }

        var combined = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, combined, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, combined, NonceSize + TagSize, cipher.Length);
        return Prefix + Convert.ToBase64String(combined);
    }

    public string Decrypt(string value)
    {
        // Legacy plaintext / non-token values pass through unchanged.
        if (!IsEnabled || string.IsNullOrEmpty(value) || !value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return value;
        }

        var combined = Convert.FromBase64String(value[Prefix.Length..]);
        if (combined.Length < NonceSize + TagSize)
        {
            return value;
        }

        var nonce = combined[..NonceSize];
        var tag = combined[NonceSize..(NonceSize + TagSize)];
        var cipher = combined[(NonceSize + TagSize)..];
        var plain = new byte[cipher.Length];

        using (var aes = new AesGcm(_key!, TagSize))
        {
            aes.Decrypt(nonce, cipher, tag, plain);
        }

        return Encoding.UTF8.GetString(plain);
    }

    private static byte[]? LoadKey(IConfiguration configuration, ILogger logger)
    {
        var vaultUri = configuration["FieldEncryption:KeyVaultUri"] ?? configuration["KeyVault:Uri"];
        var secretName = configuration["FieldEncryption:KeySecretName"] ?? "field-encryption-key";

        if (!string.IsNullOrEmpty(vaultUri) &&
            string.Equals(configuration["FieldEncryption:Source"], "KeyVault", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
                var base64 = client.GetSecret(secretName).Value.Value;
                return Convert.FromBase64String(base64);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load field-encryption key from Key Vault {VaultUri}", vaultUri);
                throw;
            }
        }

        var configKey = configuration["FieldEncryption:Key"];
        return string.IsNullOrEmpty(configKey) ? null : Convert.FromBase64String(configKey);
    }
}
