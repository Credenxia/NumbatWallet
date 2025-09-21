using Microsoft.EntityFrameworkCore;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Services;

public class WalletService : IWalletService
{
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WalletService(
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        IUnitOfWork unitOfWork)
    {
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<WalletDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByIdAsync(id, cancellationToken);
        return wallet != null ? MapToDto(wallet) : null;
    }

    public async Task<IEnumerable<WalletDto>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var wallets = await _walletRepository.FindAsync(w => w.PersonId == personId, cancellationToken);
        return wallets.Select(MapToDto);
    }

    public async Task<IEnumerable<WalletDto>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Find person by email/userId first
        var persons = await _personRepository.FindAsync(p => p.Email.Value == userId, cancellationToken);
        var person = persons.FirstOrDefault();

        if (person == null)
            return Enumerable.Empty<WalletDto>();

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
            throw new InvalidOperationException($"Person with ID {dto.PersonId} not found");

        var wallet = Wallet.Create(dto.PersonId, dto.Name ?? "Default Wallet");

        await _walletRepository.AddAsync(wallet, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return MapToDto(wallet);
    }

    public async Task<WalletDto> UpdateAsync(Guid id, UpdateWalletDto dto, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByIdAsync(id, cancellationToken);
        if (wallet == null)
            throw new InvalidOperationException($"Wallet with ID {id} not found");

        if (dto.Name != null)
            wallet.UpdateName(dto.Name);

        if (dto.Status != null && dto.Status == "SUSPENDED")
            wallet.Suspend("Admin update");
        else if (dto.Status != null && dto.Status == "ACTIVE")
            wallet.Reactivate();

        await _walletRepository.UpdateAsync(wallet, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return MapToDto(wallet);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByIdAsync(id, cancellationToken);
        if (wallet == null)
            return false;

        await _walletRepository.DeleteAsync(wallet, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SuspendAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByIdAsync(id, cancellationToken);
        if (wallet == null)
            return false;

        wallet.Suspend(reason);
        await _walletRepository.UpdateAsync(wallet, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByIdAsync(id, cancellationToken);
        if (wallet == null)
            return false;

        wallet.Reactivate();
        await _walletRepository.UpdateAsync(wallet, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    private static WalletDto MapToDto(Wallet wallet)
    {
        return new WalletDto
        {
            Id = wallet.Id,
            PersonId = wallet.PersonId,
            Did = wallet.Did?.Value ?? string.Empty,
            Name = wallet.Name,
            Status = wallet.Status.ToString(),
            CreatedAt = wallet.CreatedAt,
            UpdatedAt = wallet.UpdatedAt,
            LastAccessedAt = wallet.LastAccessedAt
        };
    }
}