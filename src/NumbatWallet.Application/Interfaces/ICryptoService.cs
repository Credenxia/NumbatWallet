using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Provides encryption and decryption services using envelope encryption
/// </summary>
public interface ICryptoService
{
    /// <summary>
    /// Encrypts plaintext using envelope encryption based on data classification
    /// </summary>
    Task<string> EncryptAsync(string plaintext, DataClassification classification);

    /// <summary>
    /// Decrypts ciphertext using envelope encryption
    /// </summary>
    Task<string> DecryptAsync(string ciphertext, DataClassification classification);

    /// <summary>
    /// Encrypts binary data using envelope encryption
    /// </summary>
    Task<byte[]> EncryptBytesAsync(byte[] plainBytes, DataClassification classification);

    /// <summary>
    /// Decrypts binary data using envelope encryption
    /// </summary>
    Task<byte[]> DecryptBytesAsync(byte[] cipherBytes, DataClassification classification);

    /// <summary>
    /// Rotates the Data Encryption Key for a tenant
    /// </summary>
    Task RotateDekAsync(string tenantId);

    /// <summary>
    /// Gets the current DEK version for a tenant
    /// </summary>
    Task<int> GetCurrentDekVersionAsync(string tenantId);
}