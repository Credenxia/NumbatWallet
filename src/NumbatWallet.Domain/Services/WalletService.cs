using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Domain.ValueObjects;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Utilities;

namespace NumbatWallet.Domain.Services;

public interface IWalletDomainService
{
    Task<Wallet> CreateWalletAsync(Guid personId, string tenantId, CancellationToken cancellationToken = default);
    Task<bool> CanCreateWalletAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<bool> ValidateWalletStatusTransitionAsync(Wallet wallet, WalletStatus newStatus, CancellationToken cancellationToken = default);
    Task<string> GenerateWalletDidAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<bool> IsWalletDidUniqueAsync(string walletDid, CancellationToken cancellationToken = default);
}

public class WalletDomainService : IWalletDomainService
{
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;

    public WalletDomainService(
        IWalletRepository walletRepository,
        IPersonRepository personRepository)
    {
        _walletRepository = walletRepository;
        _personRepository = personRepository;
    }

    public async Task<Wallet> CreateWalletAsync(Guid personId, string tenantId, CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmptyGuid(personId, nameof(personId));
        Guard.AgainstNullOrWhiteSpace(tenantId, nameof(tenantId));

        // Check if person exists and is verified
        var person = await _personRepository.GetByIdAsync(personId, cancellationToken);
        if (person == null)
        {
            throw new InvalidOperationException($"Person with ID {personId} not found");
        }

        if (!person.IsVerified)
        {
            throw new InvalidOperationException("Cannot create wallet for unverified person");
        }

        // Check if person already has an active wallet
        var canCreate = await CanCreateWalletAsync(personId, cancellationToken);
        if (!canCreate)
        {
            throw new InvalidOperationException("Person already has an active wallet");
        }

        // Generate wallet name
        var walletName = $"Wallet_{personId}_{DateTimeOffset.UtcNow.Ticks}";

        // Create wallet
        var walletResult = Wallet.Create(personId, walletName);

        if (!walletResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to create wallet: {walletResult.Error?.Message}");
        }

        var wallet = walletResult.Value;

        // Set tenant ID
        wallet.SetTenantId(tenantId);

        // Add creation event
        wallet.AddDomainEvent(new WalletCreatedEvent(
            wallet.Id,
            wallet.PersonId,
            wallet.TenantId,
            wallet.WalletDid,
            DateTimeOffset.UtcNow));

        return wallet;
    }

    public async Task<bool> CanCreateWalletAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var wallets = await _walletRepository.GetByPersonIdAsync(personId, cancellationToken);

        // Check if person has any active wallet
        return !wallets.Any(w => w.Status == WalletStatus.Active);
    }

    public Task<bool> ValidateWalletStatusTransitionAsync(Wallet wallet, WalletStatus newStatus, CancellationToken cancellationToken = default)
    {
        var currentStatus = wallet.Status;

        var isValid = (currentStatus, newStatus) switch
        {
            // Active transitions
            (WalletStatus.Active, WalletStatus.Suspended) => true,
            (WalletStatus.Active, WalletStatus.Revoked) => true,

            // Suspended transitions
            (WalletStatus.Suspended, WalletStatus.Active) => true,
            (WalletStatus.Suspended, WalletStatus.Revoked) => true,

            // Inactive transitions
            (WalletStatus.Inactive, WalletStatus.Active) => true,

            // Revoked is terminal - no transitions allowed
            (WalletStatus.Revoked, _) => false,

            // Expired can only be renewed (which creates a new wallet)
            (WalletStatus.Expired, _) => false,

            // Same status is not a transition
            _ when currentStatus == newStatus => false,

            // All other transitions are invalid
            _ => false
        };

        return Task.FromResult(isValid);
    }

    public async Task<string> GenerateWalletDidAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        // Generate a DID using the tenant's method
        var method = tenantId.ToLowerInvariant().Replace(" ", "");
        var uniqueId = Guid.NewGuid().ToString("N");

        return await Task.FromResult($"did:{method}:{uniqueId}");
    }

    public async Task<bool> IsWalletDidUniqueAsync(string walletDid, CancellationToken cancellationToken = default)
    {
        var existing = await _walletRepository.GetByDidAsync(walletDid, cancellationToken);
        return existing == null;
    }

    private async Task<string> GenerateUniqueWalletDidAsync(string tenantId, CancellationToken cancellationToken)
    {
        string walletDid;
        int attempts = 0;
        const int maxAttempts = 5;

        do
        {
            walletDid = await GenerateWalletDidAsync(tenantId, cancellationToken);
            var isUnique = await IsWalletDidUniqueAsync(walletDid, cancellationToken);

            if (isUnique)
            {
                return walletDid;
            }

            attempts++;
        } while (attempts < maxAttempts);

        throw new InvalidOperationException($"Failed to generate unique wallet DID after {maxAttempts} attempts");
    }
}