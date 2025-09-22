using Microsoft.EntityFrameworkCore;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Services;

public class WalletService : IWalletService
{
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WalletService(
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        ICredentialRepository credentialRepository,
        IUnitOfWork unitOfWork)
    {
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _credentialRepository = credentialRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<WalletDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByIdAsync(id, cancellationToken);
        return wallet != null ? MapToDto(wallet) : null;
    }

    public async Task<IEnumerable<WalletDto>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement specification pattern
        var allWallets = await _walletRepository.GetAllAsync(cancellationToken);
        var wallets = allWallets.Where(w => w.PersonId == personId);
        return wallets.Select(MapToDto);
    }

    public async Task<IEnumerable<WalletDto>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Find person by email/userId first
        // TODO: Implement person lookup by email
        // var persons = await _personRepository.FindAsync(p => p.Email.Value == userId, cancellationToken);
        var persons = await _personRepository.GetAllAsync(cancellationToken);
        var person = persons.FirstOrDefault();

        if (person == null)
        {
            return Enumerable.Empty<WalletDto>();
        }

        return await GetByPersonIdAsync(person.Id, cancellationToken);
    }

    public async Task<IEnumerable<WalletDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var wallets = await _walletRepository.GetAllAsync(cancellationToken);
        return wallets.Select(MapToDto);
    }

    public async Task<WalletDto> CreateAsync(CreateWalletDto dto, CancellationToken cancellationToken = default)
    {
        var person = await _personRepository.GetByIdAsync(dto.PersonId, cancellationToken);
        if (person == null)
        {
            throw new InvalidOperationException($"Person with ID {dto.PersonId} not found");
        }

        var walletResult = Wallet.Create(dto.PersonId, dto.Name ?? "Default Wallet");

        if (!walletResult.IsSuccess)
        {
            throw new InvalidOperationException(walletResult.Error.Message);
        }

        await _walletRepository.AddAsync(walletResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(walletResult.Value);
    }

    public async Task<WalletDto> UpdateAsync(Guid id, UpdateWalletDto dto, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByIdAsync(id, cancellationToken);
        if (wallet == null)
        {
            throw new InvalidOperationException($"Wallet with ID {id} not found");
        }

        if (dto.Name != null)
        {
            wallet.UpdateName(dto.Name);
        }

        if (dto.Status != null && dto.Status == "SUSPENDED")
        {
            wallet.Suspend("Admin update");
        }
        else if (dto.Status != null && dto.Status == "ACTIVE")
        {
            wallet.Reactivate();
        }

        await _walletRepository.UpdateAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(wallet);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByIdAsync(id, cancellationToken);
        if (wallet == null)
        {
            return false;
        }

        await _walletRepository.DeleteAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SuspendAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByIdAsync(id, cancellationToken);
        if (wallet == null)
        {
            return false;
        }

        wallet.Suspend(reason);
        await _walletRepository.UpdateAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByIdAsync(id, cancellationToken);
        if (wallet == null)
        {
            return false;
        }

        wallet.Reactivate();
        await _walletRepository.UpdateAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UserHasAccessAsync(string userId, Guid walletId, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken);
        if (wallet == null)
        {
            return false;
        }

        // Check if the user owns this wallet through person
        var person = await _personRepository.GetByIdAsync(wallet.PersonId, cancellationToken);
        if (person == null)
        {
            return false;
        }

        // Check if the user's external ID matches
        return person.ExternalId == userId;
    }

    public async Task<Dictionary<string, object>> GetWalletStatisticsAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken);
        if (wallet == null)
        {
            return new Dictionary<string, object>();
        }

        // Get credential count for this wallet
        var credentials = await _credentialRepository.GetByWalletIdAsync(walletId, cancellationToken);
        var credentialList = credentials.ToList();

        return new Dictionary<string, object>
        {
            ["WalletId"] = walletId,
            ["Status"] = wallet.Status.ToString(),
            ["CreatedAt"] = wallet.CreatedAt,
            ["TotalCredentials"] = credentialList.Count,
            ["ActiveCredentials"] = credentialList.Count(c => c.Status == CredentialStatus.Active),
            ["SuspendedCredentials"] = credentialList.Count(c => c.Status == CredentialStatus.Suspended),
            ["RevokedCredentials"] = credentialList.Count(c => c.Status == CredentialStatus.Revoked),
            ["ExpiredCredentials"] = credentialList.Count(c => c.ExpiresAt < DateTimeOffset.UtcNow)
        };
    }

    public async Task<bool> LockWalletAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken);
        if (wallet == null)
        {
            return false;
        }

        // TODO: Implement wallet locking logic
        // For now, just suspend the wallet
        wallet.Suspend("Wallet locked");
        await _walletRepository.UpdateAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnlockWalletAsync(Guid walletId, string passphrase, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken);
        if (wallet == null)
        {
            return false;
        }

        // TODO: Implement passphrase verification
        // For now, just reactivate the wallet
        wallet.Reactivate();
        await _walletRepository.UpdateAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static WalletDto MapToDto(Wallet wallet)
    {
        return new WalletDto
        {
            Id = wallet.Id.ToString(),
            PersonId = wallet.PersonId.ToString(),
            PersonName = "Unknown", // TODO: Get from repository
            Name = wallet.Name,
            Status = wallet.Status.ToString(),
            IsActive = wallet.Status == WalletStatus.Active,
            IsSuspended = wallet.Status == WalletStatus.Suspended,
            CreatedAt = wallet.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow, // Wallet doesn't have UpdatedAt
            CredentialCount = 0 // TODO: Get from repository
        };
    }
}