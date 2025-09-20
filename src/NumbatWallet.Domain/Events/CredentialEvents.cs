using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Events;

// Credential Events
public record CredentialIssuedEvent(
    Guid CredentialId,
    Guid WalletId,
    Guid IssuerId,
    string CredentialType,
    string CredentialDid,
    DateTimeOffset IssuedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public CredentialIssuedEvent(
        Guid credentialId,
        Guid walletId,
        Guid issuerId,
        string credentialType,
        string credentialDid,
        DateTimeOffset issuedAt)
        : this(credentialId, walletId, issuerId, credentialType, credentialDid, issuedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record CredentialAcceptedEvent(
    Guid CredentialId,
    Guid WalletId,
    DateTimeOffset AcceptedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public CredentialAcceptedEvent(
        Guid credentialId,
        Guid walletId,
        DateTimeOffset acceptedAt)
        : this(credentialId, walletId, acceptedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record CredentialRejectedEvent(
    Guid CredentialId,
    Guid WalletId,
    string Reason,
    DateTimeOffset RejectedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public CredentialRejectedEvent(
        Guid credentialId,
        Guid walletId,
        string reason,
        DateTimeOffset rejectedAt)
        : this(credentialId, walletId, reason, rejectedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record CredentialRevokedEvent(
    Guid CredentialId,
    Guid IssuerId,
    string Reason,
    string? RevocationRegistryId,
    DateTimeOffset RevokedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public CredentialRevokedEvent(
        Guid credentialId,
        Guid issuerId,
        string reason,
        string? revocationRegistryId,
        DateTimeOffset revokedAt)
        : this(credentialId, issuerId, reason, revocationRegistryId, revokedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record CredentialSuspendedEvent(
    Guid CredentialId,
    Guid IssuerId,
    string Reason,
    DateTimeOffset SuspendedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public CredentialSuspendedEvent(
        Guid credentialId,
        Guid issuerId,
        string reason,
        DateTimeOffset suspendedAt)
        : this(credentialId, issuerId, reason, suspendedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record CredentialReactivatedEvent(
    Guid CredentialId,
    Guid IssuerId,
    DateTimeOffset ReactivatedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public CredentialReactivatedEvent(
        Guid credentialId,
        Guid issuerId,
        DateTimeOffset reactivatedAt)
        : this(credentialId, issuerId, reactivatedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record CredentialExpiredEvent(
    Guid CredentialId,
    DateTimeOffset ExpiredAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public CredentialExpiredEvent(
        Guid credentialId,
        DateTimeOffset expiredAt)
        : this(credentialId, expiredAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record CredentialRenewedEvent(
    Guid OldCredentialId,
    Guid NewCredentialId,
    DateTimeOffset RenewedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public CredentialRenewedEvent(
        Guid oldCredentialId,
        Guid newCredentialId,
        DateTimeOffset renewedAt)
        : this(oldCredentialId, newCredentialId, renewedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record CredentialVerifiedEvent(
    Guid CredentialId,
    Guid VerifierId,
    bool VerificationResult,
    string? VerificationMethod,
    DateTimeOffset VerifiedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public CredentialVerifiedEvent(
        Guid credentialId,
        Guid verifierId,
        bool verificationResult,
        string? verificationMethod,
        DateTimeOffset verifiedAt)
        : this(credentialId, verifierId, verificationResult, verificationMethod, verifiedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record CredentialSharedEvent(
    Guid CredentialId,
    Guid WalletId,
    string VerifierDid,
    string[] SharedAttributes,
    DateTimeOffset SharedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public CredentialSharedEvent(
        Guid credentialId,
        Guid walletId,
        string verifierDid,
        string[] sharedAttributes,
        DateTimeOffset sharedAt)
        : this(credentialId, walletId, verifierDid, sharedAttributes, sharedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record CredentialUpdateRequestedEvent(
    Guid CredentialId,
    Guid IssuerId,
    string[] UpdatedFields,
    DateTimeOffset RequestedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public CredentialUpdateRequestedEvent(
        Guid credentialId,
        Guid issuerId,
        string[] updatedFields,
        DateTimeOffset requestedAt)
        : this(credentialId, issuerId, updatedFields, requestedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record CredentialUpdatedEvent(
    Guid CredentialId,
    Guid IssuerId,
    string[] UpdatedFields,
    DateTimeOffset UpdatedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public CredentialUpdatedEvent(
        Guid credentialId,
        Guid issuerId,
        string[] updatedFields,
        DateTimeOffset updatedAt)
        : this(credentialId, issuerId, updatedFields, updatedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record CredentialPresentedEvent(
    Guid CredentialId,
    Guid WalletId,
    string VerifierId,
    string Purpose,
    DateTimeOffset PresentedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public CredentialPresentedEvent(
        Guid credentialId,
        Guid walletId,
        string verifierId,
        string purpose,
        DateTimeOffset presentedAt)
        : this(credentialId, walletId, verifierId, purpose, presentedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}