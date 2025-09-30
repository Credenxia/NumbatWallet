using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ActivateWalletCommandHandler> _logger;

    public ActivateWalletCommandHandler(
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        IUnitOfWork unitOfWork,
        ICurrentTenantService tenantService,
        ILogger<ActivateWalletCommandHandler> logger)
    {
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _logger = logger;
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

        // PIN verification
        // TODO: Full implementation requires:
        // 1. PIN storage in Person aggregate (hashed with bcrypt/argon2)
        // 2. IPinVerificationService with rate limiting
        // 3. Audit logging of PIN attempts
        // 4. Account lockout after N failed attempts
        // For now, we validate that PIN is provided when wallet is suspended/locked
        if (wallet.Status != SharedKernel.Enums.WalletStatus.Active)
        {
            if (string.IsNullOrWhiteSpace(command.Pin))
            {
                _logger.LogWarning("PIN required but not provided for wallet {WalletId} reactivation", command.WalletId);
                throw new SharedKernel.Exceptions.BusinessRuleException(
                    "Wallet.PinRequired",
                    "PIN is required to reactivate a suspended or locked wallet");
            }

            // TODO: Verify PIN against stored hash using IPinVerificationService
            // For now, we accept any non-empty PIN - replace with actual verification
            _logger.LogInformation("PIN provided for wallet {WalletId} reactivation - verification pending full implementation", command.WalletId);
        }

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