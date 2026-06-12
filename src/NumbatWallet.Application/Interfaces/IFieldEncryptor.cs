namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Synchronous field-level encryptor for protecting classified data at rest. Synchronous so it
/// can be used from EF Core value converters (which must be expression-based). Implementations
/// load their key material once (e.g. from Key Vault) and perform AES-256-GCM in-process.
/// </summary>
public interface IFieldEncryptor
{
    /// <summary>True when a key is configured and encryption is active.</summary>
    bool IsEnabled { get; }

    /// <summary>Encrypt a plaintext string to an opaque, self-describing token.</summary>
    string Encrypt(string plaintext);

    /// <summary>
    /// Decrypt a token produced by <see cref="Encrypt"/>. If the input is not a recognized
    /// ciphertext token (e.g. a legacy plaintext value), it is returned unchanged so existing
    /// data remains readable without a migration.
    /// </summary>
    string Decrypt(string value);
}
