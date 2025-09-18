using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Events;

// Person Events
public record PersonCreatedEvent(
    Guid PersonId,
    string ExternalId,
    string Email,
    DateTimeOffset CreatedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PersonCreatedEvent(
        Guid personId,
        string externalId,
        string email,
        DateTimeOffset createdAt)
        : this(personId, externalId, email, createdAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PersonVerifiedEvent(
    Guid PersonId,
    string VerificationMethod,
    string? VerificationId,
    DateTimeOffset VerifiedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PersonVerifiedEvent(
        Guid personId,
        string verificationMethod,
        string? verificationId,
        DateTimeOffset verifiedAt)
        : this(personId, verificationMethod, verificationId, verifiedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PersonUpdatedEvent(
    Guid PersonId,
    string[] UpdatedFields,
    DateTimeOffset UpdatedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PersonUpdatedEvent(
        Guid personId,
        string[] updatedFields,
        DateTimeOffset updatedAt)
        : this(personId, updatedFields, updatedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PersonSuspendedEvent(
    Guid PersonId,
    string Reason,
    string SuspendedBy,
    DateTimeOffset SuspendedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PersonSuspendedEvent(
        Guid personId,
        string reason,
        string suspendedBy,
        DateTimeOffset suspendedAt)
        : this(personId, reason, suspendedBy, suspendedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PersonReactivatedEvent(
    Guid PersonId,
    string ReactivatedBy,
    DateTimeOffset ReactivatedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PersonReactivatedEvent(
        Guid personId,
        string reactivatedBy,
        DateTimeOffset reactivatedAt)
        : this(personId, reactivatedBy, reactivatedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PersonDeletedEvent(
    Guid PersonId,
    string DeletedBy,
    bool SoftDelete,
    DateTimeOffset DeletedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PersonDeletedEvent(
        Guid personId,
        string deletedBy,
        bool softDelete,
        DateTimeOffset deletedAt)
        : this(personId, deletedBy, softDelete, deletedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PersonConsentGrantedEvent(
    Guid PersonId,
    string ConsentType,
    string ConsentScope,
    DateTimeOffset GrantedAt,
    DateTimeOffset? ExpiresAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PersonConsentGrantedEvent(
        Guid personId,
        string consentType,
        string consentScope,
        DateTimeOffset grantedAt,
        DateTimeOffset? expiresAt)
        : this(personId, consentType, consentScope, grantedAt, expiresAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PersonConsentRevokedEvent(
    Guid PersonId,
    string ConsentType,
    DateTimeOffset RevokedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PersonConsentRevokedEvent(
        Guid personId,
        string consentType,
        DateTimeOffset revokedAt)
        : this(personId, consentType, revokedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PersonAuthenticationSucceededEvent(
    Guid PersonId,
    string AuthenticationMethod,
    string? SessionId,
    DateTimeOffset AuthenticatedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PersonAuthenticationSucceededEvent(
        Guid personId,
        string authenticationMethod,
        string? sessionId,
        DateTimeOffset authenticatedAt)
        : this(personId, authenticationMethod, sessionId, authenticatedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PersonAuthenticationFailedEvent(
    Guid PersonId,
    string AuthenticationMethod,
    string FailureReason,
    DateTimeOffset AttemptedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PersonAuthenticationFailedEvent(
        Guid personId,
        string authenticationMethod,
        string failureReason,
        DateTimeOffset attemptedAt)
        : this(personId, authenticationMethod, failureReason, attemptedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PersonPasswordChangedEvent(
    Guid PersonId,
    DateTimeOffset ChangedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PersonPasswordChangedEvent(
        Guid personId,
        DateTimeOffset changedAt)
        : this(personId, changedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PersonMfaEnabledEvent(
    Guid PersonId,
    string MfaMethod,
    DateTimeOffset EnabledAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PersonMfaEnabledEvent(
        Guid personId,
        string mfaMethod,
        DateTimeOffset enabledAt)
        : this(personId, mfaMethod, enabledAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PersonMfaDisabledEvent(
    Guid PersonId,
    DateTimeOffset DisabledAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PersonMfaDisabledEvent(
        Guid personId,
        DateTimeOffset disabledAt)
        : this(personId, disabledAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}