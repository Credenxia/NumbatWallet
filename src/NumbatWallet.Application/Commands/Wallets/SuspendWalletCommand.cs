using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Repositories;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Wallets;

public record SuspendWalletCommand : ICommand<bool>
{
    public required string WalletId { get; init; }
    public required string Reason { get; init; }
    public DateTimeOffset? SuspendedUntil { get; init; }
}

public class SuspendWalletCommandHandler : ICommandHandler<SuspendWalletCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWalletRepository _walletRepository;
    private readonly ILogger<SuspendWalletCommandHandler> _logger;

    public SuspendWalletCommandHandler(
        IUnitOfWork unitOfWork,
        IWalletRepository walletRepository,
        ILogger<SuspendWalletCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _walletRepository = walletRepository;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(
        SuspendWalletCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Suspending wallet {WalletId}", command.WalletId);

        var walletId = Guid.Parse(command.WalletId);
        var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken);

        if (wallet == null)
        {
            throw new EntityNotFoundException("Wallet", command.WalletId);
        }

        if (wallet.Status == SharedKernel.Enums.WalletStatus.Suspended)
        {
            _logger.LogWarning("Wallet {WalletId} is already suspended", command.WalletId);
            return false;
        }

        // Suspend the wallet
        var suspendResult = wallet.Suspend(command.Reason);
        if (suspendResult.IsFailure)
        {
            throw new DomainValidationException(suspendResult.Error.Message);
        }

        // Update wallet
        await _walletRepository.UpdateAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Wallet {WalletId} suspended successfully", wallet.Id);

        return true;
    }
}