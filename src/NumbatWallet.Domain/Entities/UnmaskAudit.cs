using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Domain.Entities;

/// <summary>
/// Unmask operation audit entity for sensitive data access tracking
/// Immutable append-only records for regulatory compliance (Privacy Act, GDPR)
/// </summary>
public class UnmaskAudit
{
    private UnmaskAudit() { } // For EF Core

    public UnmaskAudit(
        string entityType,
        string entityId,
        string fieldName,
        DataClassification classification,
        string reason,
        string userId,
        Guid tenantId,
        int durationSeconds)
    {
        Id = Guid.NewGuid();
        EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        EntityId = entityId ?? throw new ArgumentNullException(nameof(entityId));
        FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        Classification = classification;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        TenantId = tenantId;
        UnmaskedAt = DateTimeOffset.UtcNow;
        DurationSeconds = durationSeconds;
        ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(durationSeconds);
    }

    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string FieldName { get; private set; } = string.Empty;
    public DataClassification Classification { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }
    public DateTimeOffset UnmaskedAt { get; private set; }
    public int DurationSeconds { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? ApprovalReference { get; private set; }

    /// <summary>
    /// Sets the IP address and user agent from the request
    /// </summary>
    public void SetRequestContext(string? ipAddress, string? userAgent)
    {
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    /// <summary>
    /// Sets the approval reference for compliance tracking
    /// </summary>
    public void SetApprovalReference(string? approvalReference)
    {
        ApprovalReference = approvalReference;
    }

    /// <summary>
    /// Checks if the unmask operation has expired
    /// </summary>
    public bool IsExpired() => DateTimeOffset.UtcNow >= ExpiresAt;
}
