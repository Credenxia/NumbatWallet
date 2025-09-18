using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Events;

// Wallet Events
public record WalletCreatedEvent(
    Guid WalletId,
    Guid PersonId,
    string TenantId,
    string WalletDid,
    DateTimeOffset CreatedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public WalletCreatedEvent(
        Guid walletId,
        Guid personId,
        string tenantId,
        string walletDid,
        DateTimeOffset createdAt)
        : this(walletId, personId, tenantId, walletDid, createdAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record WalletActivatedEvent(
    Guid WalletId,
    string ActivatedBy,
    DateTimeOffset ActivatedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public WalletActivatedEvent(
        Guid walletId,
        string activatedBy,
        DateTimeOffset activatedAt)
        : this(walletId, activatedBy, activatedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record WalletSuspendedEvent(
    Guid WalletId,
    string Reason,
    string SuspendedBy,
    DateTimeOffset SuspendedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public WalletSuspendedEvent(
        Guid walletId,
        string reason,
        string suspendedBy,
        DateTimeOffset suspendedAt)
        : this(walletId, reason, suspendedBy, suspendedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record WalletRevokedEvent(
    Guid WalletId,
    string Reason,
    string RevokedBy,
    DateTimeOffset RevokedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public WalletRevokedEvent(
        Guid walletId,
        string reason,
        string revokedBy,
        DateTimeOffset revokedAt)
        : this(walletId, reason, revokedBy, revokedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record WalletRestoredEvent(
    Guid WalletId,
    string RestoredBy,
    DateTimeOffset RestoredAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public WalletRestoredEvent(
        Guid walletId,
        string restoredBy,
        DateTimeOffset restoredAt)
        : this(walletId, restoredBy, restoredAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record WalletBackupCreatedEvent(
    Guid WalletId,
    string BackupLocation,
    DateTimeOffset BackupAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public WalletBackupCreatedEvent(
        Guid walletId,
        string backupLocation,
        DateTimeOffset backupAt)
        : this(walletId, backupLocation, backupAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record WalletRecoveryInitiatedEvent(
    Guid WalletId,
    string RecoveryMethod,
    DateTimeOffset InitiatedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public WalletRecoveryInitiatedEvent(
        Guid walletId,
        string recoveryMethod,
        DateTimeOffset initiatedAt)
        : this(walletId, recoveryMethod, initiatedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record WalletRecoveryCompletedEvent(
    Guid WalletId,
    DateTimeOffset CompletedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public WalletRecoveryCompletedEvent(
        Guid walletId,
        DateTimeOffset completedAt)
        : this(walletId, completedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}