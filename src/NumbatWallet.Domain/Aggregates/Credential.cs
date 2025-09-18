using NumbatWallet.SharedKernel.Primitives;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Results;
using NumbatWallet.SharedKernel.Guards;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Attributes;

namespace NumbatWallet.Domain.Aggregates;

public sealed class Credential : AuditableEntity<Guid>, ITenantAware
{
    public Guid WalletId { get; private set; }
    public Guid IssuerId { get; private set; }

    [DataClassification(DataClassification.Official, "Credential")]
    public string CredentialType { get; private set; }

    [DataClassification(DataClassification.Protected, "Credential")]
    public string CredentialData { get; private set; }

    [DataClassification(DataClassification.Official, "Credential")]
    public string SchemaId { get; private set; }

    public CredentialStatus Status { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevocationReason { get; private set; }
    public string? SuspensionReason { get; private set; }
    public string TenantId { get; set; } = string.Empty;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Credential() : base(Guid.Empty)
    {
        // Required for EF Core
    }
#pragma warning restore CS8618

    private Credential(
        Guid walletId,
        Guid issuerId,
        string credentialType,
        string credentialData,
        string schemaId)
        : base(Guid.NewGuid())
    {
        WalletId = walletId;
        IssuerId = issuerId;
        CredentialType = credentialType;
        CredentialData = credentialData;
        SchemaId = schemaId;
        Status = CredentialStatus.Pending;
        IssuedAt = DateTimeOffset.UtcNow;
    }

    public static Result<Credential> Create(
        Guid walletId,
        Guid issuerId,
        string credentialType,
        string credentialData,
        string schemaId)
    {
        try
        {
            Guard.AgainstEmptyGuid(walletId, nameof(walletId));
            Guard.AgainstEmptyGuid(issuerId, nameof(issuerId));
            Guard.AgainstNullOrWhiteSpace(credentialType, nameof(credentialType));
            Guard.AgainstNullOrWhiteSpace(credentialData, nameof(credentialData));
            Guard.AgainstNullOrWhiteSpace(schemaId, nameof(schemaId));

            var credential = new Credential(
                walletId,
                issuerId,
                credentialType,
                credentialData,
                schemaId);

            return Result.Success(credential);
        }
        catch (ArgumentException ex)
        {
            return DomainError.Validation("Credential.Invalid", ex.Message);
        }
    }

    public Result Activate()
    {
        if (Status == CredentialStatus.Active)
        {
            return DomainError.BusinessRule("Credential.AlreadyActive", "Credential is already active.");
        }

        if (Status == CredentialStatus.Revoked)
        {
            return DomainError.BusinessRule("Credential.Revoked", "Cannot activate a revoked credential.");
        }

        if (IsExpired())
        {
            return DomainError.BusinessRule("Credential.Expired", "Cannot activate an expired credential.");
        }

        Status = CredentialStatus.Active;
        return Result.Success();
    }

    public Result Suspend(string reason)
    {
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));

        if (Status == CredentialStatus.Revoked)
        {
            return DomainError.BusinessRule("Credential.Revoked", "Cannot suspend a revoked credential.");
        }

        if (Status == CredentialStatus.Suspended)
        {
            return DomainError.BusinessRule("Credential.AlreadySuspended", "Credential is already suspended.");
        }

        Status = CredentialStatus.Suspended;
        SuspensionReason = reason;
        return Result.Success();
    }

    public Result Revoke(string reason)
    {
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));

        if (Status == CredentialStatus.Revoked)
        {
            return DomainError.BusinessRule("Credential.AlreadyRevoked", "Credential is already revoked.");
        }

        Status = CredentialStatus.Revoked;
        RevokedAt = DateTimeOffset.UtcNow;
        RevocationReason = reason;
        return Result.Success();
    }

    public Result SetExpiry(DateTimeOffset expiryDate)
    {
        ExpiresAt = expiryDate;

        if (IsExpired() && Status == CredentialStatus.Active)
        {
            Status = CredentialStatus.Expired;
        }

        return Result.Success();
    }

    public Result UpdateData(string newData)
    {
        Guard.AgainstNullOrWhiteSpace(newData, nameof(newData));

        if (Status == CredentialStatus.Revoked)
        {
            return DomainError.BusinessRule("Credential.Revoked", "Cannot update data of a revoked credential.");
        }

        CredentialData = newData;
        return Result.Success();
    }

    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;
    }

    public bool IsActive()
    {
        return Status == CredentialStatus.Active && !IsExpired();
    }
}