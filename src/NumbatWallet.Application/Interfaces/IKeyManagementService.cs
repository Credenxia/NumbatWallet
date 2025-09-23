namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for cryptographic key management
/// </summary>
public interface IKeyManagementService
{
    /// <summary>
    /// Generate a new key
    /// </summary>
    Task<KeyInfo> GenerateKeyAsync(GenerateKeyRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotate an existing key
    /// </summary>
    Task<KeyRotationResult> RotateKeyAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// List all keys
    /// </summary>
    Task<List<KeyInfo>> ListKeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get key by ID
    /// </summary>
    Task<KeyInfo?> GetKeyAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a key
    /// </summary>
    Task<bool> DeleteKeyAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export key (if allowed)
    /// </summary>
    Task<KeyExportData> ExportKeyAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Import key
    /// </summary>
    Task<KeyInfo> ImportKeyAsync(KeyImportData data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get key rotation schedule
    /// </summary>
    Task<KeyRotationSchedule> GetRotationScheduleAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update key rotation schedule
    /// </summary>
    Task<bool> UpdateRotationScheduleAsync(string keyId, KeyRotationSchedule schedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotate keys of a specific type (for GraphQL)
    /// </summary>
    Task<KeyRotationResultDto> RotateKeysAsync(KeyType keyType, CancellationToken cancellationToken = default);
}

// DTOs for key management
public class KeyRotationResultDto
{
    public bool Success { get; set; }
    public int KeysRotated { get; set; }
    public List<string> NewKeyIds { get; set; } = new();
    public DateTime CompletedAt { get; set; }
}

public enum KeyType
{
    Encryption,
    Signing,
    All
}

public class KeyInfo
{
    public string KeyId { get; set; } = string.Empty;
    public string KeyName { get; set; } = string.Empty;
    public string KeyType { get; set; } = string.Empty;
    public int KeySize { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastRotatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class GenerateKeyRequest
{
    public string KeyName { get; set; } = string.Empty;
    public string KeyType { get; set; } = string.Empty;
    public int KeySize { get; set; } = 2048;
    public bool Exportable { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class KeyRotationResult
{
    public string OldKeyId { get; set; } = string.Empty;
    public string NewKeyId { get; set; } = string.Empty;
    public DateTime RotatedAt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class KeyExportData
{
    public string KeyId { get; set; } = string.Empty;
    public byte[] KeyMaterial { get; set; } = Array.Empty<byte>();
    public string Format { get; set; } = string.Empty;
    public DateTime ExportedAt { get; set; }
}

public class KeyImportData
{
    public string KeyName { get; set; } = string.Empty;
    public byte[] KeyMaterial { get; set; } = Array.Empty<byte>();
    public string Format { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class KeyRotationSchedule
{
    public string KeyId { get; set; } = string.Empty;
    public bool AutoRotateEnabled { get; set; }
    public int RotationIntervalDays { get; set; }
    public DateTime? NextRotationDate { get; set; }
    public int WarningDaysBefore { get; set; } = 7;
}
