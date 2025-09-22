using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NumbatWallet.Domain.Interfaces;

/// <summary>
/// Service for managing automated key rotation policies
/// POA-131: Implement key rotation policies
/// </summary>
public interface IKeyRotationService
{
    /// <summary>
    /// Rotate a specific key based on its policy
    /// </summary>
    Task<RotationResult> RotateKeyAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check and enforce rotation policies for all managed keys
    /// </summary>
    Task<IEnumerable<RotationResult>> EnforceRotationPoliciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule a key for rotation at a specific time
    /// </summary>
    Task<bool> ScheduleRotationAsync(string keyId, DateTime rotationTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the rotation policy for a specific key
    /// </summary>
    Task<RotationPolicy> GetRotationPolicyAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the rotation policy for a specific key
    /// </summary>
    Task<bool> UpdateRotationPolicyAsync(string keyId, RotationPolicy policy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get keys that are due for rotation
    /// </summary>
    Task<IEnumerable<KeyRotationStatus>> GetPendingRotationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get keys currently in grace period
    /// </summary>
    Task<IEnumerable<KeyRotationStatus>> GetKeysInGracePeriodAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Perform emergency rotation for a compromised key
    /// </summary>
    Task<RotationResult> EmergencyRotateAsync(string keyId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback a key rotation if issues are detected
    /// </summary>
    Task<bool> RollbackRotationAsync(string rotationId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate compliance report for key rotations
    /// </summary>
    Task<ComplianceReport> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a key rotation operation
/// </summary>
public class RotationResult
{
    public bool Success { get; set; }
    public string RotationId { get; set; } = string.Empty;
    public string OldKeyId { get; set; } = string.Empty;
    public string NewKeyId { get; set; } = string.Empty;
    public DateTime RotatedAt { get; set; }
    public DateTime GracePeriodEnds { get; set; }
    public string? ErrorMessage { get; set; }
    public RotationType Type { get; set; }
}

/// <summary>
/// Key rotation policy configuration
/// </summary>
public class RotationPolicy
{
    public string KeyId { get; set; } = string.Empty;
    public RotatableKeyType KeyType { get; set; }
    public int RotationIntervalDays { get; set; }
    public int GracePeriodDays { get; set; }
    public int WarningDays { get; set; }
    public bool AutoRotateEnabled { get; set; }
    public DateTime? NextRotationDate { get; set; }
    public DateTime? LastRotatedDate { get; set; }
    public int MinimumKeyAgeDays { get; set; }
    public int MaximumKeyAgeDays { get; set; }
    public string? NotificationEmail { get; set; }
    public ComplianceLevel RequiredComplianceLevel { get; set; }
}

/// <summary>
/// Current status of a key regarding rotation
/// </summary>
public class KeyRotationStatus
{
    public string KeyId { get; set; } = string.Empty;
    public string KeyName { get; set; } = string.Empty;
    public RotatableKeyType KeyType { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastRotatedDate { get; set; }
    public DateTime? NextRotationDate { get; set; }
    public int CurrentAgeDays { get; set; }
    public RotationState State { get; set; }
    public bool IsInGracePeriod { get; set; }
    public DateTime? GracePeriodEnds { get; set; }
    public string? WarningMessage { get; set; }
}

/// <summary>
/// Compliance report for key rotations
/// </summary>
public class ComplianceReport
{
    public DateTime GeneratedAt { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalKeys { get; set; }
    public int KeysRotated { get; set; }
    public int OverdueRotations { get; set; }
    public int EmergencyRotations { get; set; }
    public int FailedRotations { get; set; }
    public double CompliancePercentage { get; set; }
    public List<RotationAuditEntry> AuditEntries { get; set; } = new();
    public Dictionary<RotatableKeyType, ComplianceStatistics> StatisticsByKeyType { get; set; } = new();
}

/// <summary>
/// Audit entry for a key rotation
/// </summary>
public class RotationAuditEntry
{
    public string RotationId { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public DateTime RotatedAt { get; set; }
    public RotationType Type { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
}

/// <summary>
/// Compliance statistics for a specific key type
/// </summary>
public class ComplianceStatistics
{
    public RotatableKeyType KeyType { get; set; }
    public int TotalKeys { get; set; }
    public int CompliantKeys { get; set; }
    public int NonCompliantKeys { get; set; }
    public double AverageKeyAge { get; set; }
    public int RotationsPerformed { get; set; }
}

/// <summary>
/// Types of keys that can be rotated
/// </summary>
public enum RotatableKeyType
{
    SigningKey,
    EncryptionKey,
    TlsCertificate,
    ApiKey,
    HsmMasterKey,
    DataEncryptionKey,
    KeyEncryptionKey
}

/// <summary>
/// Type of rotation performed
/// </summary>
public enum RotationType
{
    Scheduled,
    Manual,
    Emergency,
    PolicyDriven,
    Compliance
}

/// <summary>
/// Current state of a key in rotation lifecycle
/// </summary>
public enum RotationState
{
    Active,
    DueForRotation,
    InGracePeriod,
    Overdue,
    Rotating,
    Decommissioned
}

/// <summary>
/// Compliance level requirements
/// </summary>
public enum ComplianceLevel
{
    None,
    Low,
    Medium,
    High,
    Critical
}