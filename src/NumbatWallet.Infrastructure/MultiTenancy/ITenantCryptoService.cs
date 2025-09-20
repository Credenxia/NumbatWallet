using System;
using System.Threading.Tasks;

namespace NumbatWallet.Infrastructure.MultiTenancy;

public interface ITenantCryptoService
{
    Task<TenantCryptoConfiguration> GetTenantCryptoConfigAsync(string tenantId);
    Task<byte[]> EncryptForTenantAsync(string tenantId, byte[] plaintext);
    Task<byte[]> DecryptForTenantAsync(string tenantId, byte[] ciphertext);
    Task<string> EncryptStringForTenantAsync(string tenantId, string plaintext);
    Task<string> DecryptStringForTenantAsync(string tenantId, string ciphertext);
    Task RotateTenantKeysAsync(string tenantId);
    Task<TenantKeyInfo> GetTenantKeyInfoAsync(string tenantId);
    Task InitializeTenantCryptoAsync(string tenantId);
    Task DestroyTenantCryptoAsync(string tenantId);
}

public class TenantCryptoConfiguration
{
    public string TenantId { get; set; } = string.Empty;
    public string KekId { get; set; } = string.Empty;
    public string CurrentDekId { get; set; } = string.Empty;
    public EncryptionAlgorithm Algorithm { get; set; } = EncryptionAlgorithm.AES256GCM;
    public KeyDerivationFunction Kdf { get; set; } = KeyDerivationFunction.PBKDF2;
    public int KdfIterations { get; set; } = 100000;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastRotation { get; set; }
    public int RotationIntervalDays { get; set; } = 90;
    public bool IsActive { get; set; } = true;
}

public class TenantKeyInfo
{
    public string TenantId { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public KeyType Type { get; set; }
    public KeyStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsed { get; set; }
    public long EncryptionCount { get; set; }
    public long DecryptionCount { get; set; }
}

public enum EncryptionAlgorithm
{
    AES256GCM,
    AES256CBC,
    ChaCha20Poly1305
}

public enum KeyDerivationFunction
{
    PBKDF2,
    Argon2id,
    Scrypt
}

public enum KeyType
{
    MasterKey,      // Tenant master key (never used directly)
    KeyEncryptionKey,   // KEK for encrypting DEKs
    DataEncryptionKey,  // DEK for encrypting data
    SigningKey,     // For digital signatures
    BackupKey       // For key escrow/recovery
}

public enum KeyStatus
{
    Active,         // Currently in use
    Rotating,       // Being rotated out
    Archived,       // No longer used but kept for decryption
    Destroyed,      // Crypto-shredded
    Compromised     // Marked as compromised
}