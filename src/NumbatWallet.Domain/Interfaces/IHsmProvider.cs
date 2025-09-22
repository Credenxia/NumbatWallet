using System;
using System.Threading;
using System.Threading.Tasks;

namespace NumbatWallet.Domain.Interfaces;

/// <summary>
/// Abstraction layer for HSM providers to support phased migration:
/// Phase 1: Software (Development) / Key Vault Premium (Production)
/// Phase 2: Azure Managed HSM
/// Phase 3: Dedicated HSM
/// </summary>
public interface IHsmProvider
{
    /// <summary>
    /// Gets the provider type identifier
    /// </summary>
    string ProviderType { get; }

    /// <summary>
    /// Indicates if this provider supports hardware-backed keys
    /// </summary>
    bool SupportsHardwareBackedKeys { get; }

    /// <summary>
    /// Gets the FIPS compliance level of this provider
    /// </summary>
    FipsComplianceLevel ComplianceLevel { get; }

    /// <summary>
    /// Checks if the provider is available and operational
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a new cryptographic key
    /// </summary>
    Task<HsmKey> GenerateKeyAsync(
        KeyGenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs data using the specified key
    /// </summary>
    Task<byte[]> SignAsync(
        string keyId,
        byte[] data,
        SigningAlgorithm algorithm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a signature using the specified key
    /// </summary>
    Task<bool> VerifyAsync(
        string keyId,
        byte[] data,
        byte[] signature,
        SigningAlgorithm algorithm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypts data using the specified key
    /// </summary>
    Task<byte[]> EncryptAsync(
        string keyId,
        byte[] plaintext,
        EncryptionAlgorithm algorithm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts data using the specified key
    /// </summary>
    Task<byte[]> DecryptAsync(
        string keyId,
        byte[] ciphertext,
        EncryptionAlgorithm algorithm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Wraps a key for secure export/storage
    /// </summary>
    Task<byte[]> WrapKeyAsync(
        string wrappingKeyId,
        byte[] keyToWrap,
        KeyWrapAlgorithm algorithm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unwraps a previously wrapped key
    /// </summary>
    Task<byte[]> UnwrapKeyAsync(
        string unwrappingKeyId,
        byte[] wrappedKey,
        KeyWrapAlgorithm algorithm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Backs up a key for disaster recovery
    /// </summary>
    Task<KeyBackupData> BackupKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a key from backup
    /// </summary>
    Task<string> RestoreKeyAsync(
        KeyBackupData backup,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a key (may be soft delete depending on provider)
    /// </summary>
    Task<bool> DeleteKeyAsync(
        string keyId,
        bool permanentDelete = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrates a key to another provider
    /// </summary>
    Task<MigrationResult> MigrateKeyAsync(
        string keyId,
        IHsmProvider targetProvider,
        MigrationOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata about a key
    /// </summary>
    Task<HsmKey> GetKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all keys managed by this provider
    /// </summary>
    Task<IEnumerable<HsmKey>> ListKeysAsync(
        string? prefix = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets provider-specific configuration
    /// </summary>
    HsmProviderConfiguration GetConfiguration();

    /// <summary>
    /// Validates provider health and connectivity
    /// </summary>
    Task<HealthCheckResult> CheckHealthAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Key generation request parameters
/// </summary>
public class KeyGenerationRequest
{
    public string KeyName { get; set; } = string.Empty;
    public KeyType KeyType { get; set; }
    public int KeySize { get; set; }
    public KeyUsage Usage { get; set; }
    public bool Exportable { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}

/// <summary>
/// Represents a key in the HSM
/// </summary>
public class HsmKey
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public KeyType Type { get; set; }
    public int KeySize { get; set; }
    public KeyUsage Usage { get; set; }
    public bool IsHardwareBacked { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public DateTime? LastUsedOn { get; set; }
    public string Version { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public Dictionary<string, string> Tags { get; set; } = new();
    public string? PublicKey { get; set; }
}

/// <summary>
/// Key backup data for disaster recovery
/// </summary>
public class KeyBackupData
{
    public string KeyId { get; set; } = string.Empty;
    public byte[] BackupBlob { get; set; } = Array.Empty<byte>();
    public string BackupVersion { get; set; } = string.Empty;
    public DateTime BackupDate { get; set; }
    public string SourceProvider { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Result of key migration operation
/// </summary>
public class MigrationResult
{
    public bool Success { get; set; }
    public string NewKeyId { get; set; } = string.Empty;
    public string SourceKeyId { get; set; } = string.Empty;
    public DateTime MigratedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public MigrationStatistics Statistics { get; set; } = new();
}

/// <summary>
/// Migration statistics
/// </summary>
public class MigrationStatistics
{
    public TimeSpan Duration { get; set; }
    public long BytesTransferred { get; set; }
    public int OperationsPerformed { get; set; }
}

/// <summary>
/// Options for key migration
/// </summary>
public class MigrationOptions
{
    public bool DeleteSourceAfterMigration { get; set; }
    public bool VerifyAfterMigration { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// HSM provider configuration
/// </summary>
public class HsmProviderConfiguration
{
    public string ProviderType { get; set; } = string.Empty;
    public string? ConnectionString { get; set; }
    public Dictionary<string, string> Settings { get; set; } = new();
    public bool CachingEnabled { get; set; }
    public TimeSpan? CacheDuration { get; set; }
}

/// <summary>
/// Health check result for HSM provider
/// </summary>
public class HealthCheckResult
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Types of cryptographic keys
/// </summary>
public enum KeyType
{
    RSA,
    EC,
    AES,
    HMAC
}

/// <summary>
/// Key usage flags
/// </summary>
[Flags]
public enum KeyUsage
{
    None = 0,
    Sign = 1,
    Verify = 2,
    Encrypt = 4,
    Decrypt = 8,
    WrapKey = 16,
    UnwrapKey = 32,
    Derive = 64,
    All = Sign | Verify | Encrypt | Decrypt | WrapKey | UnwrapKey | Derive
}

/// <summary>
/// Signing algorithms
/// </summary>
public enum SigningAlgorithm
{
    RS256,  // RSA with SHA-256
    RS384,  // RSA with SHA-384
    RS512,  // RSA with SHA-512
    PS256,  // RSA-PSS with SHA-256
    PS384,  // RSA-PSS with SHA-384
    PS512,  // RSA-PSS with SHA-512
    ES256,  // ECDSA with P-256 and SHA-256
    ES384,  // ECDSA with P-384 and SHA-384
    ES512   // ECDSA with P-521 and SHA-512
}

/// <summary>
/// Encryption algorithms
/// </summary>
public enum EncryptionAlgorithm
{
    RSA_OAEP,       // RSA with OAEP padding
    RSA_OAEP_256,   // RSA with OAEP-256 padding
    AES_GCM,        // AES in GCM mode
    AES_CBC         // AES in CBC mode
}

/// <summary>
/// Key wrap algorithms
/// </summary>
public enum KeyWrapAlgorithm
{
    RSA_OAEP,
    RSA_OAEP_256,
    AES_KW,
    AES_KWP
}

/// <summary>
/// FIPS 140-2 compliance levels
/// </summary>
public enum FipsComplianceLevel
{
    None = 0,
    Level1 = 1,    // Software-based
    Level2 = 2,    // Software with tamper-evidence
    Level3 = 3,    // Hardware with tamper-resistance
    Level4 = 4     // Hardware with complete physical protection
}