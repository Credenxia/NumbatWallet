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

        // Get person details (needed for PIN verification and DTO)
        var person = await _personRepository.GetByIdAsync(wallet.PersonId, cancellationToken);
        if (person == null || person.TenantId != tenantId)
        {
            _logger.LogWarning("Person {PersonId} not found for wallet {WalletId}", wallet.PersonId, command.WalletId);
            throw new Application.Common.Exceptions.EntityNotFoundException("Person", wallet.PersonId.ToString());
        }

        // PIN verification with rate limiting and account lockout
        if (wallet.Status != SharedKernel.Enums.WalletStatus.Active)
        {
            if (string.IsNullOrWhiteSpace(command.Pin))
            {
                _logger.LogWarning("PIN required but not provided for wallet {WalletId} reactivation", command.WalletId);
                throw new SharedKernel.Exceptions.BusinessRuleException(
                    "Wallet.PinRequired",
                    "PIN is required to reactivate a suspended or locked wallet");
            }

            // Verify PIN using the Person aggregate's built-in verification
            var verifyResult = person.VerifyPin(command.Pin);
            if (verifyResult.IsFailure)
            {
                _logger.LogWarning("PIN verification failed for wallet {WalletId}: {Error}", command.WalletId, verifyResult.Error.Message);

                // Save the failed attempt count
                await _personRepository.UpdateAsync(person, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                throw new SharedKernel.Exceptions.BusinessRuleException(verifyResult.Error.Code, verifyResult.Error.Message);
            }

            if (!verifyResult.Value)
            {
                _logger.LogWarning("Invalid PIN provided for wallet {WalletId}. Attempts: {Attempts}",
                    command.WalletId, person.FailedPinAttempts);

                // Save the failed attempt count
                await _personRepository.UpdateAsync(person, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var remainingAttempts = 5 - person.FailedPinAttempts;
                throw new SharedKernel.Exceptions.BusinessRuleException(
                    "Wallet.InvalidPin",
                    $"Invalid PIN. You have {remainingAttempts} attempt(s) remaining before account lockout");
            }

            // PIN verified successfully - save the reset attempt counter
            await _personRepository.UpdateAsync(person, cancellationToken);
            _logger.LogInformation("PIN verified successfully for wallet {WalletId} reactivation", command.WalletId);
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

        // Build DTO with person details
        var personName = $"{person.FirstName} {person.LastName}";

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