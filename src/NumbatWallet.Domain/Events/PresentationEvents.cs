using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Events;

// Presentation Events
public record PresentationRequestedEvent(
    Guid RequestId,
    string VerifierDid,
    Guid WalletId,
    string[] RequestedCredentialTypes,
    string[] RequestedAttributes,
    DateTimeOffset RequestedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PresentationRequestedEvent(
        Guid requestId,
        string verifierDid,
        Guid walletId,
        string[] requestedCredentialTypes,
        string[] requestedAttributes,
        DateTimeOffset requestedAt)
        : this(requestId, verifierDid, walletId, requestedCredentialTypes, requestedAttributes, requestedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PresentationCreatedEvent(
    Guid PresentationId,
    Guid WalletId,
    Guid RequestId,
    Guid[] IncludedCredentialIds,
    DateTimeOffset CreatedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PresentationCreatedEvent(
        Guid presentationId,
        Guid walletId,
        Guid requestId,
        Guid[] includedCredentialIds,
        DateTimeOffset createdAt)
        : this(presentationId, walletId, requestId, includedCredentialIds, createdAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PresentationSharedEvent(
    Guid PresentationId,
    Guid WalletId,
    string VerifierDid,
    string[] SharedAttributes,
    DateTimeOffset SharedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PresentationSharedEvent(
        Guid presentationId,
        Guid walletId,
        string verifierDid,
        string[] sharedAttributes,
        DateTimeOffset sharedAt)
        : this(presentationId, walletId, verifierDid, sharedAttributes, sharedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PresentationVerifiedEvent(
    Guid PresentationId,
    string VerifierDid,
    bool VerificationResult,
    string? FailureReason,
    DateTimeOffset VerifiedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PresentationVerifiedEvent(
        Guid presentationId,
        string verifierDid,
        bool verificationResult,
        string? failureReason,
        DateTimeOffset verifiedAt)
        : this(presentationId, verifierDid, verificationResult, failureReason, verifiedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PresentationRevokedEvent(
    Guid PresentationId,
    Guid WalletId,
    string Reason,
    DateTimeOffset RevokedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PresentationRevokedEvent(
        Guid presentationId,
        Guid walletId,
        string reason,
        DateTimeOffset revokedAt)
        : this(presentationId, walletId, reason, revokedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record SelectiveDisclosureAppliedEvent(
    Guid PresentationId,
    Guid CredentialId,
    string[] DisclosedAttributes,
    string[] HiddenAttributes,
    DateTimeOffset AppliedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public SelectiveDisclosureAppliedEvent(
        Guid presentationId,
        Guid credentialId,
        string[] disclosedAttributes,
        string[] hiddenAttributes,
        DateTimeOffset appliedAt)
        : this(presentationId, credentialId, disclosedAttributes, hiddenAttributes, appliedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}