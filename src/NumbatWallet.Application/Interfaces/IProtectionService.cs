using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for applying data protection based on tenant policies.
/// Handles encryption, tokenization, and redaction transparently.
/// </summary>
public interface IProtectionService
{
    /// <summary>
    /// Protects a field value based on its classification and tenant policy
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="value">The value to protect</param>
    /// <param name="classification">The data classification level</param>
    /// <param name="fieldName">The name of the field</param>
    /// <param name="entityType">The type of entity containing the field</param>
    /// <returns>A protected representation of the value</returns>
    Task<ProtectedValue<T>> ProtectAsync<T>(
        T value,
        DataClassification classification,
        string fieldName,
        string entityType);

    /// <summary>
    /// Unprotects a field value (decrypts if encrypted)
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="protectedValue">The protected value</param>
    /// <param name="reason">The reason for accessing the unprotected value</param>
    /// <returns>The unprotected value</returns>
    Task<T> UnprotectAsync<T>(
        ProtectedValue<T> protectedValue,
        string? reason = null);

    /// <summary>
    /// Generates search tokens for a field value
    /// </summary>
    /// <param name="value">The value to tokenize</param>
    /// <param name="fieldName">The name of the field</param>
    /// <param name="strategy">The search strategy to use</param>
    /// <returns>Array of search tokens</returns>
    Task<string[]> GenerateSearchTokensAsync(
        string value,
        string fieldName,
        SearchStrategy strategy);

    /// <summary>
    /// Gets the redacted display value for a field
    /// </summary>
    /// <param name="value">The original value</param>
    /// <param name="pattern">The redaction pattern to apply</param>
    /// <returns>The redacted display value</returns>
    string GetRedactedValue(string value, RedactionPattern pattern);
}

/// <summary>
/// Represents a protected value that may be encrypted, tokenized, or plaintext
/// based on tenant policy
/// </summary>
public class ProtectedValue<T>
{
    /// <summary>
    /// The plaintext value (null if encrypted)
    /// </summary>
    public T? PlainValue { get; set; }

    /// <summary>
    /// The encrypted value (null if not encrypted)
    /// </summary>
    public EncryptedData? EncryptedValue { get; set; }

    /// <summary>
    /// The redacted display value (always present for sensitive data)
    /// </summary>
    public string RedactedDisplay { get; set; } = "****";

    /// <summary>
    /// Search tokens for the value
    /// </summary>
    public List<string> SearchTokens { get; set; } = new();

    /// <summary>
    /// The data classification level
    /// </summary>
    public DataClassification Classification { get; set; }

    /// <summary>
    /// Whether the value is currently encrypted
    /// </summary>
    public bool IsEncrypted => EncryptedValue != null;
}

/// <summary>
/// Represents encrypted data with metadata
/// </summary>
public class EncryptedData
{
    /// <summary>
    /// The encrypted bytes
    /// </summary>
    public byte[] CipherText { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// The ID of the key used for encryption
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// The encryption algorithm used
    /// </summary>
    public string Algorithm { get; set; } = "AES-256-GCM";

    /// <summary>
    /// When the data was encrypted
    /// </summary>
    public DateTimeOffset EncryptedAt { get; set; }
}