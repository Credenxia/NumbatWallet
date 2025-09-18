using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Infrastructure.Data.Converters;

/// <summary>
/// Value converter for storing protected fields as JSONB in PostgreSQL
/// </summary>
public class ProtectedFieldConverter : ValueConverter<string, string>
{
    public ProtectedFieldConverter()
        : base(
            value => ConvertToJson(value),
            json => ConvertFromJson(json))
    {
    }

    public new string ConvertToProviderTyped(string value) => ConvertToJson(value);
    public new string ConvertFromProviderTyped(string json) => ConvertFromJson(json);

    private static string ConvertToJson(string value)
    {
        // For now, store as simple JSON structure
        // In production, this would integrate with IProtectionService
        var protectedData = new ProtectedFieldData
        {
            Value = value,
            Encrypted = null,
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
            return protectedData?.Value ?? string.Empty;
        }
        catch
        {
            // If it's not JSON, assume it's a plain string (for backward compatibility)
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