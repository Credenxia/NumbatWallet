using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Repositories;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Wallets;

public record DeleteWalletCommand : ICommand<bool>
{
    public required string WalletId { get; init; }
    public bool HardDelete { get; init; } = false;
    public string? DeletionReason { get; init; }
}

public class DeleteWalletCommandHandler : ICommandHandler<DeleteWalletCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWalletRepository _walletRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly ILogger<DeleteWalletCommandHandler> _logger;

    public DeleteWalletCommandHandler(
        IUnitOfWork unitOfWork,
        IWalletRepository walletRepository,
        ICredentialRepository credentialRepository,
        ILogger<DeleteWalletCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _walletRepository = walletRepository;
        _credentialRepository = credentialRepository;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(
        DeleteWalletCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting wallet {WalletId} (HardDelete: {HardDelete})", 
            command.WalletId, command.HardDelete);

        var walletId = Guid.Parse(command.WalletId);
        var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken);

        if (wallet == null)
        {
            throw new EntityNotFoundException("Wallet", command.WalletId);
        }

        // Check for active credentials
        var credentials = await _credentialRepository.FindAsync(
            new Domain.Specifications.CredentialByWalletSpecification(walletId),
            cancellationToken);

        if (credentials.Any(c => c.Status != NumbatWallet.SharedKernel.Enums.CredentialStatus.Revoked && !c.IsExpired()))
        {
            throw new DomainValidationException(
                "Cannot delete wallet with active credentials. Revoke or expire all credentials first.");
        }

        if (command.HardDelete)
        {
            // Hard delete - remove from database
            await _walletRepository.DeleteAsync(wallet, cancellationToken);
            _logger.LogInformation("Wallet {WalletId} hard deleted", wallet.Id);
        }
        else
        {
            // Soft delete - suspend the wallet with deletion reason
            var suspendResult = wallet.Suspend(command.DeletionReason ?? "Deleted");
            if (suspendResult.IsFailure)
            {
                throw new DomainValidationException(suspendResult.Error.Message);
            }

            await _walletRepository.UpdateAsync(wallet, cancellationToken);
            _logger.LogInformation("Wallet {WalletId} soft deleted", wallet.Id);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}