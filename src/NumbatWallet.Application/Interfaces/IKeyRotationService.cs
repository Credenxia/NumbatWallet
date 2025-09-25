namespace NumbatWallet.Application.Interfaces;

public interface IKeyRotationService
{
    Task<IEnumerable<KeyRotationDto>> GetActiveKeysAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<RotationScheduleDto>> GetRotationSchedulesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<RotationHistoryDto>> GetRotationHistoryAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<KeyRotationDto> RotateKeyAsync(string keyId, CancellationToken cancellationToken = default);
    Task<bool> ActivateKeyAsync(string keyId, CancellationToken cancellationToken = default);
    Task<bool> DeactivateKeyAsync(string keyId, CancellationToken cancellationToken = default);
    Task<bool> UpdateScheduleAsync(RotationScheduleDto schedule, CancellationToken cancellationToken = default);
    Task RotateAllKeysAsync(CancellationToken cancellationToken = default);
    Task RotateKeysByTypeAsync(string keyType, CancellationToken cancellationToken = default);
}

public class KeyRotationDto
{
    public string KeyId { get; set; } = string.Empty;
    public string KeyType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastRotatedAt { get; set; }
    public DateTime? NextRotation { get; set; }
}

public class RotationScheduleDto
{
    public string KeyType { get; set; } = string.Empty;
    public int RotationPeriodDays { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? LastRotation { get; set; }
    public DateTime? NextRotation { get; set; }
}

public class RotationHistoryDto
{
    public string KeyType { get; set; } = string.Empty;
    public DateTime RotatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RotatedBy { get; set; }
}