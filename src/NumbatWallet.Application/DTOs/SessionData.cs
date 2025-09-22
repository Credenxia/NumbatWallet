namespace NumbatWallet.Application.DTOs;

/// <summary>
/// Represents session data stored in distributed cache
/// </summary>
public class SessionData
{
    public string? SessionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, string> Claims { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastAccessedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public string? DeviceId { get; set; }
    public string? RefreshToken { get; set; }
}

/// <summary>
/// Represents a device session for persistent authentication
/// </summary>
public class DeviceSession
{
    public string DeviceId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string? DeviceFingerprint { get; set; }
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsTrusted { get; set; }
    public string? PublicKey { get; set; } // For device-based authentication
}