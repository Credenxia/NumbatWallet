using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Events;

// Issuer Events
public record IssuerRegisteredEvent(
    Guid IssuerId,
    string IssuerDid,
    string Name,
    string[] SupportedCredentialTypes,
    DateTimeOffset RegisteredAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public IssuerRegisteredEvent(
        Guid issuerId,
        string issuerDid,
        string name,
        string[] supportedCredentialTypes,
        DateTimeOffset registeredAt)
        : this(issuerId, issuerDid, name, supportedCredentialTypes, registeredAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record IssuerVerifiedEvent(
    Guid IssuerId,
    string VerificationMethod,
    string? TrustRegistryId,
    DateTimeOffset VerifiedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public IssuerVerifiedEvent(
        Guid issuerId,
        string verificationMethod,
        string? trustRegistryId,
        DateTimeOffset verifiedAt)
        : this(issuerId, verificationMethod, trustRegistryId, verifiedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record IssuerTrustedEvent(
    Guid IssuerId,
    string TrustRegistryId,
    int TrustLevel,
    DateTimeOffset TrustedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public IssuerTrustedEvent(
        Guid issuerId,
        string trustRegistryId,
        int trustLevel,
        DateTimeOffset trustedAt)
        : this(issuerId, trustRegistryId, trustLevel, trustedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record IssuerUntrustedEvent(
    Guid IssuerId,
    string Reason,
    DateTimeOffset UntrustedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public IssuerUntrustedEvent(
        Guid issuerId,
        string reason,
        DateTimeOffset untrustedAt)
        : this(issuerId, reason, untrustedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record IssuerSuspendedEvent(
    Guid IssuerId,
    string Reason,
    string SuspendedBy,
    DateTimeOffset SuspendedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public IssuerSuspendedEvent(
        Guid issuerId,
        string reason,
        string suspendedBy,
        DateTimeOffset suspendedAt)
        : this(issuerId, reason, suspendedBy, suspendedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record IssuerReactivatedEvent(
    Guid IssuerId,
    string ReactivatedBy,
    DateTimeOffset ReactivatedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public IssuerReactivatedEvent(
        Guid issuerId,
        string reactivatedBy,
        DateTimeOffset reactivatedAt)
        : this(issuerId, reactivatedBy, reactivatedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record IssuerCredentialSchemaAddedEvent(
    Guid IssuerId,
    string CredentialType,
    string SchemaId,
    string SchemaVersion,
    DateTimeOffset AddedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public IssuerCredentialSchemaAddedEvent(
        Guid issuerId,
        string credentialType,
        string schemaId,
        string schemaVersion,
        DateTimeOffset addedAt)
        : this(issuerId, credentialType, schemaId, schemaVersion, addedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record IssuerCredentialSchemaRemovedEvent(
    Guid IssuerId,
    string CredentialType,
    DateTimeOffset RemovedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public IssuerCredentialSchemaRemovedEvent(
        Guid issuerId,
        string credentialType,
        DateTimeOffset removedAt)
        : this(issuerId, credentialType, removedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record IssuerCredentialSchemaUpdatedEvent(
    Guid IssuerId,
    string CredentialType,
    string NewSchemaId,
    string NewSchemaVersion,
    DateTimeOffset UpdatedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public IssuerCredentialSchemaUpdatedEvent(
        Guid issuerId,
        string credentialType,
        string newSchemaId,
        string newSchemaVersion,
        DateTimeOffset updatedAt)
        : this(issuerId, credentialType, newSchemaId, newSchemaVersion, updatedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record IssuerRevocationRegistryCreatedEvent(
    Guid IssuerId,
    string RegistryId,
    string CredentialType,
    int MaxCredentials,
    DateTimeOffset CreatedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public IssuerRevocationRegistryCreatedEvent(
        Guid issuerId,
        string registryId,
        string credentialType,
        int maxCredentials,
        DateTimeOffset createdAt)
        : this(issuerId, registryId, credentialType, maxCredentials, createdAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record IssuerRevocationRegistryFullEvent(
    Guid IssuerId,
    string RegistryId,
    DateTimeOffset FullAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public IssuerRevocationRegistryFullEvent(
        Guid issuerId,
        string registryId,
        DateTimeOffset fullAt)
        : this(issuerId, registryId, fullAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record IssuerCertificateUpdatedEvent(
    Guid IssuerId,
    string CertificateThumbprint,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    DateTimeOffset UpdatedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public IssuerCertificateUpdatedEvent(
        Guid issuerId,
        string certificateThumbprint,
        DateTimeOffset validFrom,
        DateTimeOffset validTo,
        DateTimeOffset updatedAt)
        : this(issuerId, certificateThumbprint, validFrom, validTo, updatedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record IssuerCertificateExpiringEvent(
    Guid IssuerId,
    string CertificateThumbprint,
    DateTimeOffset ExpiresAt,
    int DaysRemaining,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public IssuerCertificateExpiringEvent(
        Guid issuerId,
        string certificateThumbprint,
        DateTimeOffset expiresAt,
        int daysRemaining)
        : this(issuerId, certificateThumbprint, expiresAt, daysRemaining, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}