using Microsoft.EntityFrameworkCore;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.SharedKernel.Exceptions;
using NumbatWallet.Domain.Specifications;

namespace NumbatWallet.Application.Commands.Wallets.Handlers;

public class DeleteWalletCommandHandler : ICommandHandler<DeleteWalletCommand, bool>
{
    private readonly IWalletRepository _walletRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _tenantService;

    public DeleteWalletCommandHandler(
        IWalletRepository walletRepository,
        ICredentialRepository credentialRepository,
        IUnitOfWork unitOfWork,
        ICurrentTenantService tenantService)
    {
        _walletRepository = walletRepository;
        _credentialRepository = credentialRepository;
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
    }

    public async Task<bool> HandleAsync(
        DeleteWalletCommand command,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.TenantId;

        var wallet = await _walletRepository.GetByIdAsync(command.WalletId, cancellationToken);

        if (wallet == null || wallet.TenantId != tenantId)
        {
            throw new Application.Common.Exceptions.EntityNotFoundException("Wallet", command.WalletId.ToString());
        }

        // Check if wallet has any credentials using specification
        var credentialSpec = new CredentialByWalletSpecification(command.WalletId);
        var credentials = await _credentialRepository.FindAsync(credentialSpec, cancellationToken);

        if (credentials.Any())
        {
            throw new SharedKernel.Exceptions.BusinessRuleException("WALLET_HAS_CREDENTIALS", "Cannot delete wallet with existing credentials. Remove all credentials first.");
        }

        // Lock the wallet instead of hard delete (soft delete pattern)
        var result = wallet.Lock("Deleted by user");

        if (result.IsFailure)
        {
            throw new SharedKernel.Exceptions.BusinessRuleException(result.Error.Code, result.Error.Message);
        }

        // Save changes
        await _walletRepository.UpdateAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}