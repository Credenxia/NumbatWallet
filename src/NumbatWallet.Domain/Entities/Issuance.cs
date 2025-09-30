using NumbatWallet.Domain.ValueObjects;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Primitives;

namespace NumbatWallet.Domain.Entities;

/// <summary>
/// Represents an issuance request for a verifiable credential
/// </summary>
public class Issuance : Entity<Guid>, ITenantEntity
{
    private readonly Dictionary<string, object> _claims = new();
    private readonly Dictionary<string, string> _metadata = new();

    public Guid TenantId { get; private set; }
    public string CredentialType { get; private set; }
    public Guid WalletId { get; private set; }
    public string RequesterId { get; private set; }
    public string Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public string? RejectedBy { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? CompletedBy { get; private set; }
    public Guid? CredentialId { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public IReadOnlyDictionary<string, object> Claims => _claims;
    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    // Navigation properties - these will be added once the entities exist
    // public virtual Wallet? Wallet { get; private set; }
    // public virtual Credential? Credential { get; private set; }

    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core constructor
    protected Issuance() : base(Guid.Empty) { }

    public Issuance(
        Guid tenantId,
        string credentialType,
        Guid walletId,
        string requesterId,
        Dictionary<string, object>? claims = null,
        DateTime? expiryDate = null) : base(Guid.NewGuid())
    {
        ArgumentNullException.ThrowIfNull(credentialType);
        ArgumentNullException.ThrowIfNull(requesterId);

        TenantId = tenantId;
        CredentialType = credentialType;
        WalletId = walletId;
        RequesterId = requesterId;
        Status = IssuanceStatus.Pending;
        RequestedAt = DateTime.UtcNow;
        ExpiryDate = expiryDate;
        CreatedAt = DateTime.UtcNow;

        if (claims != null)
        {
            foreach (var claim in claims)
            {
                _claims[claim.Key] = claim.Value;
            }
        }
    }

    /// <summary>
    /// Approve the issuance request
    /// </summary>
    public void Approve(string approvedBy, string? comments = null)
    {
        ArgumentNullException.ThrowIfNull(approvedBy);

        if (Status != IssuanceStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot approve issuance in {Status} status");
        }

        Status = IssuanceStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedBy = approvedBy;
        UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(comments))
        {
            AddMetadata("approval_comments", comments);
        }

        AddDomainEvent(new IssuanceApprovedEvent(Id, WalletId, CredentialType, approvedBy));
    }

    /// <summary>
    /// Reject the issuance request
    /// </summary>
    public void Reject(string rejectedBy, string reason, string? comments = null)
    {
        ArgumentNullException.ThrowIfNull(rejectedBy);
        ArgumentNullException.ThrowIfNull(reason);

        if (Status != IssuanceStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot reject issuance in {Status} status");
        }

        Status = IssuanceStatus.Rejected;
        RejectedAt = DateTime.UtcNow;
        RejectedBy = rejectedBy;
        RejectionReason = reason;
        UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(comments))
        {
            AddMetadata("rejection_comments", comments);
        }

        AddDomainEvent(new IssuanceRejectedEvent(Id, WalletId, CredentialType, rejectedBy, reason));
    }

    /// <summary>
    /// Complete the issuance (credential has been issued)
    /// </summary>
    public void Complete(string completedBy, Guid credentialId, string? comments = null)
    {
        ArgumentNullException.ThrowIfNull(completedBy);

        if (Status != IssuanceStatus.Approved)
        {
            throw new InvalidOperationException($"Cannot complete issuance in {Status} status. Must be approved first.");
        }

        Status = IssuanceStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        CompletedBy = completedBy;
        CredentialId = credentialId;
        UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(comments))
        {
            AddMetadata("completion_comments", comments);
        }

        AddDomainEvent(new IssuanceCompletedEvent(Id, WalletId, credentialId, CredentialType, completedBy));
    }

    /// <summary>
    /// Cancel the issuance request
    /// </summary>
    public void Cancel(string cancelledBy, string reason)
    {
        ArgumentNullException.ThrowIfNull(cancelledBy);
        ArgumentNullException.ThrowIfNull(reason);

        if (Status == IssuanceStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel a completed issuance");
        }

        Status = IssuanceStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        AddMetadata("cancelled_by", cancelledBy);
        AddMetadata("cancellation_reason", reason);
        AddMetadata("cancelled_at", DateTime.UtcNow.ToString("O"));

        AddDomainEvent(new IssuanceCancelledEvent(Id, WalletId, CredentialType, cancelledBy, reason));
    }

    /// <summary>
    /// Add or update metadata
    /// </summary>
    public void AddMetadata(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        _metadata[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update claims
    /// </summary>
    public void UpdateClaims(Dictionary<string, object> claims)
    {
        ArgumentNullException.ThrowIfNull(claims);

        if (Status != IssuanceStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot update claims for issuance in {Status} status");
        }

        _claims.Clear();
        foreach (var claim in claims)
        {
            _claims[claim.Key] = claim.Value;
        }

        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Issuance status values
/// </summary>
public static class IssuanceStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
}

// Domain Events
public record IssuanceApprovedEvent(Guid IssuanceId, Guid WalletId, string CredentialType, string ApprovedBy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public record IssuanceRejectedEvent(Guid IssuanceId, Guid WalletId, string CredentialType, string RejectedBy, string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public record IssuanceCompletedEvent(Guid IssuanceId, Guid WalletId, Guid CredentialId, string CredentialType, string CompletedBy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public record IssuanceCancelledEvent(Guid IssuanceId, Guid WalletId, string CredentialType, string CancelledBy, string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}