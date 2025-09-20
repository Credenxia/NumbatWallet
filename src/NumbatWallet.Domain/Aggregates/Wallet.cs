using NumbatWallet.Domain.Events;
using NumbatWallet.SharedKernel.Primitives;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Results;
using NumbatWallet.SharedKernel.Guards;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Attributes;

namespace NumbatWallet.Domain.Aggregates;

public sealed class Wallet : AuditableEntity<Guid>, ITenantAware
{
    private readonly List<Guid> _credentialIds = new();

    public Guid PersonId { get; private set; }
    public string TenantId { get; private set; } = string.Empty;

    [DataClassification(DataClassification.Official, "Wallet")]
    public string WalletName { get; private set; }

    public string Name => WalletName; // Alias for compatibility

    [DataClassification(DataClassification.Official, "Wallet")]
    public string WalletDid { get; private set; }
    public WalletStatus Status { get; private set; }
    public string? SuspensionReason { get; private set; }
    public string? LockReason { get; private set; }
    public string? ExternalId { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public IReadOnlyCollection<Guid> GetCredentials() => _credentialIds.AsReadOnly();
    public IReadOnlyCollection<Credential> Credentials { get; } = new List<Credential>();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Wallet() : base(Guid.Empty)
    {
        // Required for persistence
    }
#pragma warning restore CS8618

    private Wallet(
        Guid personId,
        string tenantId,
        string walletName)
        : base(Guid.NewGuid())
    {
        PersonId = personId;
        TenantId = tenantId;
        WalletName = walletName;
        WalletDid = GenerateWalletDid();
        Status = WalletStatus.Active;
    }

    public static Result<Wallet> Create(
        Guid personId,
        string walletName)
    {
        try
        {
            Guard.AgainstEmptyGuid(personId, nameof(personId));
            Guard.AgainstNullOrWhiteSpace(walletName, nameof(walletName));

            var wallet = new Wallet(
                personId,
                string.Empty, // Will be set by persistence layer
                walletName);

            // Raise domain event
            wallet.AddDomainEvent(new WalletCreatedEvent(
                wallet.Id,
                wallet.PersonId,
                wallet.TenantId,
                wallet.WalletDid,
                DateTimeOffset.UtcNow));

            return Result.Success(wallet);
        }
        catch (ArgumentException ex)
        {
            return DomainError.Validation("Wallet.Invalid", ex.Message);
        }
    }

    public void SetTenantId(string tenantId)
    {
        Guard.AgainstNullOrWhiteSpace(tenantId, nameof(tenantId));
        TenantId = tenantId;
    }

    public Result AddCredential(Guid credentialId)
    {
        Guard.AgainstEmptyGuid(credentialId, nameof(credentialId));

        if (_credentialIds.Contains(credentialId))
        {
            return DomainError.BusinessRule("Wallet.CredentialExists", "Credential already exists in wallet.");
        }

        if (Status != WalletStatus.Active)
        {
            return DomainError.BusinessRule("Wallet.NotActive", "Cannot add credentials to inactive wallet.");
        }

        _credentialIds.Add(credentialId);
        return Result.Success();
    }

    public Result RemoveCredential(Guid credentialId)
    {
        Guard.AgainstEmptyGuid(credentialId, nameof(credentialId));

        if (!_credentialIds.Contains(credentialId))
        {
            return DomainError.BusinessRule("Wallet.CredentialNotFound", "Credential not found in wallet.");
        }

        _credentialIds.Remove(credentialId);
        return Result.Success();
    }

    public Result Suspend(string reason)
    {
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));

        if (Status == WalletStatus.Suspended)
        {
            return DomainError.BusinessRule("Wallet.AlreadySuspended", "Wallet is already suspended.");
        }

        if (Status == WalletStatus.Locked)
        {
            return DomainError.BusinessRule("Wallet.Locked", "Cannot suspend a locked wallet.");
        }

        Status = WalletStatus.Suspended;
        SuspensionReason = reason;
        return Result.Success();
    }

    public Result Reactivate()
    {
        if (Status == WalletStatus.Active)
        {
            return DomainError.BusinessRule("Wallet.AlreadyActive", "Wallet is already active.");
        }

        if (Status == WalletStatus.Locked)
        {
            return DomainError.BusinessRule("Wallet.Locked", "Cannot reactivate a locked wallet. Unlock it first.");
        }

        Status = WalletStatus.Active;
        SuspensionReason = null;
        return Result.Success();
    }

    public Result Lock(string reason)
    {
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));

        if (Status == WalletStatus.Locked)
        {
            return DomainError.BusinessRule("Wallet.AlreadyLocked", "Wallet is already locked.");
        }

        Status = WalletStatus.Locked;
        LockReason = reason;
        return Result.Success();
    }

    public Result Unlock()
    {
        if (Status != WalletStatus.Locked)
        {
            return DomainError.BusinessRule("Wallet.NotLocked", "Wallet is not locked.");
        }

        Status = WalletStatus.Active;
        LockReason = null;
        return Result.Success();
    }

    public Result UpdateName(string newName)
    {
        Guard.AgainstNullOrWhiteSpace(newName, nameof(newName));

        if (WalletName == newName)
        {
            return DomainError.BusinessRule("Wallet.SameName", "New name is the same as current name.");
        }

        WalletName = newName;
        return Result.Success();
    }

    public int GetCredentialCount() => _credentialIds.Count;

    public bool HasCredential(Guid credentialId) => _credentialIds.Contains(credentialId);

    private static string GenerateWalletDid()
    {
        // Generate a DID for the wallet
        // In production, this would use a proper DID method
        return $"did:wa:wallet:{Guid.NewGuid():N}";
    }
}