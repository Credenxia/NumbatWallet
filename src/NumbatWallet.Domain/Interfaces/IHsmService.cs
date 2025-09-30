using System.Security.Cryptography.X509Certificates;

namespace NumbatWallet.Domain.Interfaces;

/// <summary>
/// Interface for Hardware Security Module (HSM) operations
/// </summary>
public interface IHsmService
{
    /// <summary>
    /// Generate a new key pair in the HSM
    /// </summary>
    Task<string> GenerateKeyPairAsync(string keyName, KeyAlgorithm algorithm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sign data using an HSM-protected key
    /// </summary>
    Task<byte[]> SignDataAsync(string keyName, byte[] data, SignatureAlgorithm algorithm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify signature using an HSM-protected key
    /// </summary>
    Task<bool> VerifySignatureAsync(string keyName, byte[] data, byte[] signature, SignatureAlgorithm algorithm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypt data using an HSM-protected key
    /// </summary>
    Task<byte[]> EncryptDataAsync(string keyName, byte[] plaintext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypt data using an HSM-protected key
    /// </summary>
    Task<byte[]> DecryptDataAsync(string keyName, byte[] ciphertext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wrap a key for secure export
    /// </summary>
    Task<byte[]> WrapKeyAsync(string wrappingKeyName, byte[] keyToWrap, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unwrap a previously wrapped key
    /// </summary>
    Task<byte[]> UnwrapKeyAsync(string wrappingKeyName, byte[] wrappedKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a key from the HSM
    /// </summary>
    Task<bool> DeleteKeyAsync(string keyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotate a key in the HSM
    /// </summary>
    Task<string> RotateKeyAsync(string keyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get public key from HSM
    /// </summary>
    Task<byte[]> GetPublicKeyAsync(string keyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a certificate signing request
    /// </summary>
    Task<byte[]> CreateCertificateSigningRequestAsync(string keyName, X500DistinguishedName subjectName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Import a certificate to the HSM
    /// </summary>
    Task<bool> ImportCertificateAsync(string keyName, X509Certificate2 certificate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get key metadata
    /// </summary>
    Task<HsmKeyMetadata> GetKeyMetadataAsync(string keyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// List all keys in the HSM
    /// </summary>
    Task<IEnumerable<string>> ListKeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check HSM health status
    /// </summary>
    Task<HsmHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Supported key algorithms
/// </summary>
public enum KeyAlgorithm
{
    RSA2048,
    RSA3072,
    RSA4096,
    ECC_P256,
    ECC_P384,
    ECC_P521,
    AES128,
    AES256
}

/// <summary>
/// Supported signature algorithms
/// </summary>
public enum SignatureAlgorithm
{
    RS256,  // RSA with SHA-256
    RS384,  // RSA with SHA-384
    RS512,  // RSA with SHA-512
    ES256,  // ECDSA with P-256 and SHA-256
    ES384,  // ECDSA with P-384 and SHA-384
    ES512,  // ECDSA with P-521 and SHA-512
    PS256,  // RSA PSS with SHA-256
    PS384,  // RSA PSS with SHA-384
    PS512   // RSA PSS with SHA-512
}

/// <summary>
/// HSM key metadata
/// </summary>
public class HsmKeyMetadata
{
    public string KeyId { get; set; } = string.Empty;
    public string KeyName { get; set; } = string.Empty;
    public KeyAlgorithm Algorithm { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string Version { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public List<string> AllowedOperations { get; set; } = new();
}

/// <summary>
/// HSM health status
/// </summary>
public class HsmHealthStatus
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
    public DateTimeOffset CheckedAt { get; set; }
}
