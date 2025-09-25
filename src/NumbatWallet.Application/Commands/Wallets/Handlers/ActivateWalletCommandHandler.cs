using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.SharedKernel.Exceptions;

namespace NumbatWallet.Application.Commands.Wallets.Handlers;

public class ActivateWalletCommandHandler : ICommandHandler<ActivateWalletCommand, WalletDto>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _tenantService;

    public ActivateWalletCommandHandler(
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        IUnitOfWork unitOfWork,
        ICurrentTenantService tenantService)
    {
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
    }

    public async Task<WalletDto> HandleAsync(
        ActivateWalletCommand command,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.TenantId;

        var wallet = await _walletRepository.GetByIdAsync(command.WalletId, cancellationToken);

        if (wallet == null || wallet.TenantId != tenantId)
        {
            throw new Application.Common.Exceptions.EntityNotFoundException("Wallet", command.WalletId.ToString());
        }

        // Verify PIN if required and provided
        // TODO: Implement PIN verification when security layer is ready

        // Reactivate the wallet using the domain method
        var result = wallet.Reactivate();

        if (result.IsFailure)
        {
            throw new SharedKernel.Exceptions.BusinessRuleException(result.Error.Code, result.Error.Message);
        }

        // Save changes
        await _walletRepository.UpdateAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get person details for the DTO
        var person = await _personRepository.GetByIdAsync(wallet.PersonId, cancellationToken);
        var personName = person != null ? $"{person.FirstName} {person.LastName}" : "Unknown";

        return new WalletDto
        {
            Id = wallet.Id.ToString(),
            PersonId = wallet.PersonId.ToString(),
            PersonName = personName,
            Name = wallet.WalletName,
            Status = wallet.Status.ToString(),
            IsActive = wallet.Status == SharedKernel.Enums.WalletStatus.Active,
            IsSuspended = wallet.Status == SharedKernel.Enums.WalletStatus.Suspended,
            CreatedAt = wallet.CreatedAt,
            UpdatedAt = wallet.ModifiedAt ?? wallet.CreatedAt,
            CredentialCount = wallet.GetCredentials().Count,
            Metadata = new Dictionary<string, string>
            {
                ["Type"] = wallet.Type.ToString(),
                ["DID"] = wallet.WalletDid,
                ["TenantId"] = wallet.TenantId
            }
        };
    }
}