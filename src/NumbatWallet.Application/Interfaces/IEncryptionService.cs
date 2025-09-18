namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for encryption operations using envelope encryption (DEK/KEK pattern)
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts data using the current key for the tenant
    /// </summary>
    /// <param name="plaintext">The data to encrypt</param>
    /// <param name="keyId">The key ID to use for encryption</param>
    /// <returns>The encrypted data</returns>
    Task<byte[]> EncryptAsync(byte[] plaintext, string keyId);

    /// <summary>
    /// Decrypts data using the specified key
    /// </summary>
    /// <param name="ciphertext">The encrypted data</param>
    /// <param name="keyId">The key ID used for encryption</param>
    /// <returns>The decrypted data</returns>
    Task<byte[]> DecryptAsync(byte[] ciphertext, string keyId);

    /// <summary>
    /// Gets the current encryption key ID for the tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>The current key ID</returns>
    Task<string> GetCurrentKeyIdAsync(string tenantId);

    /// <summary>
    /// Generates an HMAC token for searchable encryption
    /// </summary>
    /// <param name="data">The data to generate HMAC for</param>
    /// <param name="context">Additional context for the HMAC (e.g., field name)</param>
    /// <returns>The HMAC token</returns>
    Task<string> GenerateHmacAsync(string data, string context);

    /// <summary>
    /// Rotates encryption keys for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="reEncryptData">Whether to re-encrypt existing data</param>
    /// <returns>The new key ID</returns>
    Task<string> RotateKeysAsync(string tenantId, bool reEncryptData = false);

    /// <summary>
    /// Checks if a key is valid and not expired
    /// </summary>
    /// <param name="keyId">The key ID to check</param>
    /// <returns>True if the key is valid</returns>
    Task<bool> IsKeyValidAsync(string keyId);
}