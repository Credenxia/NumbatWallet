using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Infrastructure.Data.Converters;

/// <summary>
/// Value converter for storing protected fields as JSONB in PostgreSQL. When a field encryptor
/// is configured the value is AES-256-GCM encrypted at rest (ciphertext stored, plaintext never
/// persisted); otherwise it falls back to storing the plaintext (with a redacted preview).
///
/// The encryptor is supplied via a static accessor because EF Core value converters are created
/// during model building and cannot take scoped DI dependencies. It is a process-wide singleton
/// set once at startup (see AddInfrastructure).
/// </summary>
public class ProtectedFieldConverter : ValueConverter<string, string>
{
    /// <summary>Process-wide field encryptor, set once during DI configuration. Null = disabled.</summary>
    public static IFieldEncryptor? FieldEncryptor { get; set; }

    private readonly bool _encrypt;

    /// <param name="encrypt">
    /// When true (default), the value is encrypted at rest if an encryptor is configured. Set to
    /// FALSE for identifier/searchable fields (e.g. email, phone) that must remain queryable —
    /// encrypting them would null the searchable value and break lookups. Searchable encryption
    /// requires deterministic search tokens (see ISearchTokenService), a future enhancement.
    /// </param>
    public ProtectedFieldConverter(bool encrypt = true)
        : base(
            value => ConvertToJson(value, encrypt),
            json => ConvertFromJson(json))
    {
        _encrypt = encrypt;
    }

    public new string ConvertToProviderTyped(string value) => ConvertToJson(value, _encrypt);
    public new string ConvertFromProviderTyped(string json) => ConvertFromJson(json);

    private static string ConvertToJson(string value, bool encrypt)
    {
        var encryptor = FieldEncryptor;
        var encryptionOn = encrypt && encryptor is { IsEnabled: true };

        var protectedData = new ProtectedFieldData
        {
            // When encryption is on, NEVER store the plaintext — only the ciphertext.
            Value = encryptionOn ? null : value,
            Encrypted = encryptionOn
                ? new EncryptedFieldData
                {
                    CipherText = encryptor!.Encrypt(value),
                    Algorithm = "AES-256-GCM",
                    EncryptedAt = DateTimeOffset.UtcNow
                }
                : null,
            Redacted = GetRedactedValue(value),
            Classification = DataClassification.OfficialSensitive
        };

        return JsonSerializer.Serialize(protectedData);
    }

    private static string ConvertFromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return string.Empty;
        }

        try
        {
            var protectedData = JsonSerializer.Deserialize<ProtectedFieldData>(json);
            if (protectedData is null)
            {
                return string.Empty;
            }

            // Encrypted form takes precedence; decrypt it.
            if (!string.IsNullOrEmpty(protectedData.Encrypted?.CipherText))
            {
                var encryptor = FieldEncryptor;
                return encryptor is { IsEnabled: true }
                    ? encryptor.Decrypt(protectedData.Encrypted.CipherText)
                    : protectedData.Encrypted.CipherText; // can't decrypt; return as-is rather than throw
            }

            // Legacy / unencrypted form.
            return protectedData.Value ?? string.Empty;
        }
        catch
        {
            // If it's not JSON, assume it's a plain string (backward compatibility).
            return json;
        }
    }

    private static string GetRedactedValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Length <= 1)
        {
            return "*";
        }

        // Default redaction: show first and last character
        return $"{value[0]}{new string('*', Math.Min(value.Length - 2, 5))}{value[^1]}";
    }
}

/// <summary>
/// Internal structure for JSONB storage
/// </summary>
internal class ProtectedFieldData
{
    public string? Value { get; set; }
    public EncryptedFieldData? Encrypted { get; set; }
    public string Redacted { get; set; } = "****";
    public DataClassification Classification { get; set; }
}

internal class EncryptedFieldData
{
    public string CipherText { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "AES-256-GCM";
    public DateTimeOffset EncryptedAt { get; set; }
}
