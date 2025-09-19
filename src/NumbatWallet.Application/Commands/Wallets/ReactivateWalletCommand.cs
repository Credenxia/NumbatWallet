using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Repositories;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Wallets;

public record ReactivateWalletCommand : ICommand<WalletDto>
{
    public required string WalletId { get; init; }
    public string? ReactivationReason { get; init; }
}

public class ReactivateWalletCommandHandler : ICommandHandler<ReactivateWalletCommand, WalletDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly ILogger<ReactivateWalletCommandHandler> _logger;

    public ReactivateWalletCommandHandler(
        IUnitOfWork unitOfWork,
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        ILogger<ReactivateWalletCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _logger = logger;
    }

    public async Task<WalletDto> HandleAsync(
        ReactivateWalletCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reactivating wallet {WalletId}", command.WalletId);

        var walletId = Guid.Parse(command.WalletId);
        var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken);

        if (wallet == null)
        {
            throw new EntityNotFoundException("Wallet", command.WalletId);
        }

        if (wallet.Status != NumbatWallet.SharedKernel.Enums.WalletStatus.Suspended)
        {
            _logger.LogWarning("Wallet {WalletId} is not suspended", command.WalletId);
            throw new DomainValidationException("Cannot reactivate a wallet that is not suspended");
        }

        // Reactivate the wallet
        var reactivateResult = wallet.Reactivate();
        if (reactivateResult.IsFailure)
        {
            throw new DomainValidationException(reactivateResult.Error.Message);
        }

        // Update wallet
        await _walletRepository.UpdateAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get person details for DTO
        var person = await _personRepository.GetByIdAsync(wallet.PersonId, cancellationToken);

        _logger.LogInformation("Wallet {WalletId} reactivated successfully", wallet.Id);

        // Map to DTO
        return new WalletDto
        {
            Id = wallet.Id.ToString(),
            PersonId = wallet.PersonId.ToString(),
            PersonName = person != null ? $"{person.FirstName} {person.LastName}" : "Unknown",
            Name = wallet.Name,
            Status = wallet.Status.ToString(),
            IsActive = wallet.Status == NumbatWallet.SharedKernel.Enums.WalletStatus.Active,
            IsSuspended = wallet.Status == NumbatWallet.SharedKernel.Enums.WalletStatus.Suspended,
            CreatedAt = wallet.CreatedAt,
            UpdatedAt = wallet.CreatedAt,
            CredentialCount = 0,
            Metadata = new Dictionary<string, string>()
        };
    }
}