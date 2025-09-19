using System.Text;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

public class ProtectionService : IProtectionService
{
    private readonly IEncryptionService _encryptionService;
    private readonly ITenantPolicyService _tenantPolicyService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ProtectionService> _logger;

    public ProtectionService(
        IEncryptionService encryptionService,
        ITenantPolicyService tenantPolicyService,
        ICurrentTenantService currentTenantService,
        IAuditService auditService,
        ILogger<ProtectionService> logger)
    {
        _encryptionService = encryptionService;
        _tenantPolicyService = tenantPolicyService;
        _currentTenantService = currentTenantService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ProtectedValue<T>> ProtectAsync<T>(
        T value,
        DataClassification classification,
        string fieldName,
        string entityType)
    {
        var protectedValue = new ProtectedValue<T>
        {
            Classification = classification
        };

        var tenantId = _currentTenantService.TenantId ?? string.Empty;
        var tenantGuid = Guid.Empty;
        if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var parsed))
        {
            tenantGuid = parsed;
        }

        // Check if encryption is required based on tenant policy
        var requiresEncryption = await _tenantPolicyService.RequiresEncryptionAsync(tenantGuid, entityType, fieldName);

        if (requiresEncryption && value != null)
        {
            // Encrypt the value
            var keyId = await _encryptionService.GetCurrentKeyIdAsync(tenantId);
            var valueBytes = SerializeValue(value);
            var encryptedBytes = await _encryptionService.EncryptAsync(valueBytes, keyId);

            protectedValue.EncryptedValue = new EncryptedData
            {
                CipherText = encryptedBytes,
                KeyId = keyId,
                Algorithm = "AES-256-GCM",
                EncryptedAt = DateTimeOffset.UtcNow
            };
            protectedValue.PlainValue = default;
        }
        else
        {
            // Store as plaintext
            protectedValue.PlainValue = value;
            protectedValue.EncryptedValue = null;
        }

        // Generate redacted display value if needed
        if (classification == DataClassification.Secret || classification == DataClassification.OfficialSensitive)
        {
            var stringValue = value?.ToString() ?? string.Empty;
            protectedValue.RedactedDisplay = GetRedactedValue(stringValue, RedactionPattern.Full);
        }

        return protectedValue;
    }

    public async Task<T> UnprotectAsync<T>(
        ProtectedValue<T> protectedValue,
        string? reason = null)
    {
        if (protectedValue.EncryptedValue != null)
        {
            // Decrypt the value
            var decryptedBytes = await _encryptionService.DecryptAsync(
                protectedValue.EncryptedValue.CipherText,
                protectedValue.EncryptedValue.KeyId);

            var value = DeserializeValue<T>(decryptedBytes);

            // Log data access for audit
            if (!string.IsNullOrEmpty(reason))
            {
                await _auditService.LogUnmaskOperationAsync(
                    "ProtectedValue",
                    protectedValue.EncryptedValue.KeyId,
                    "Value",
                    protectedValue.Classification,
                    reason,
                    "CurrentUser", // Should come from ICurrentUserService
                    300); // Default duration
            }

            return value;
        }

        return protectedValue.PlainValue!;
    }

    public async Task<string[]> GenerateSearchTokensAsync(
        string value,
        string fieldName,
        SearchStrategy strategy)
    {
        var tokens = new List<string>();
        var tenantId = _currentTenantService.TenantId ?? string.Empty;
        var context = $"{tenantId}:{fieldName}";

        switch (strategy)
        {
            case SearchStrategy.Exact:
                var exactToken = await _encryptionService.GenerateHmacAsync(value, context);
                tokens.Add(exactToken);
                break;

            case SearchStrategy.Prefix:
                // Generate tokens for each prefix
                for (int i = 1; i <= value.Length; i++)
                {
                    var prefix = value.Substring(0, i);
                    var prefixToken = await _encryptionService.GenerateHmacAsync(prefix, context);
                    tokens.Add(prefixToken);
                }
                break;

            case SearchStrategy.Fuzzy:
                // For fuzzy search, we might use phonetic algorithms or n-grams
                // For now, just use exact match as a placeholder
                var fuzzyToken = await _encryptionService.GenerateHmacAsync(value, context);
                tokens.Add(fuzzyToken);
                break;
        }

        return tokens.ToArray();
    }

    public string GetRedactedValue(string value, RedactionPattern pattern)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "****";
        }

        return pattern switch
        {
            RedactionPattern.ShowLastFour => value.Length > 4
                ? string.Concat(new string('*', value.Length - 4), value.AsSpan(value.Length - 4))
                : "****",

            RedactionPattern.ShowDomain => value.Contains('@')
                ? "****@" + value.Split('@')[1]
                : "****",

            RedactionPattern.ShowFirstThree => value.Length > 3
                ? string.Concat(value.AsSpan(0, 3), new string('*', value.Length - 3))
                : "****",

            RedactionPattern.Full => "****",

            _ => "****"
        };
    }

    private byte[] SerializeValue<T>(T value)
    {
        if (value == null)
        {
            return Array.Empty<byte>();
        }

        if (value is string stringValue)
        {
            return Encoding.UTF8.GetBytes(stringValue);
        }

        // For other types, use JSON serialization or custom logic
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        return Encoding.UTF8.GetBytes(json);
    }

    private T DeserializeValue<T>(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return default!;
        }

        if (typeof(T) == typeof(string))
        {
            var stringValue = Encoding.UTF8.GetString(bytes);
            return (T)(object)stringValue;
        }

        // For other types, use JSON deserialization
        var json = Encoding.UTF8.GetString(bytes);
        return System.Text.Json.JsonSerializer.Deserialize<T>(json)!;
    }
}