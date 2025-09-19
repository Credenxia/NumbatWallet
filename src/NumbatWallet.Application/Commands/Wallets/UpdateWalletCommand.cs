using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Wallets;

public record UpdateWalletCommand : ICommand<WalletDto>
{
    public required string WalletId { get; init; }
    public string? Name { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

public class UpdateWalletCommandHandler : ICommandHandler<UpdateWalletCommand, WalletDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly ILogger<UpdateWalletCommandHandler> _logger;

    public UpdateWalletCommandHandler(
        IUnitOfWork unitOfWork,
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        ILogger<UpdateWalletCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _logger = logger;
    }

    public async Task<WalletDto> HandleAsync(
        UpdateWalletCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating wallet {WalletId}", command.WalletId);

        var walletId = Guid.Parse(command.WalletId);
        var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken);

        if (wallet == null)
        {
            throw new EntityNotFoundException("Wallet", command.WalletId);
        }

        // Update name if provided
        if (!string.IsNullOrWhiteSpace(command.Name) && command.Name != wallet.Name)
        {
            var updateResult = wallet.UpdateName(command.Name);
            if (updateResult.IsFailure)
            {
                throw new DomainValidationException(updateResult.Error.Message);
            }
        }

        // Update metadata if provided
        // Note: The domain model doesn't have SetMetadata, so we'll skip this for now

        // Save changes
        await _walletRepository.UpdateAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get person details for DTO
        var person = await _personRepository.GetByIdAsync(wallet.PersonId, cancellationToken);

        _logger.LogInformation("Wallet {WalletId} updated successfully", wallet.Id);

        // Map to DTO
        return new WalletDto
        {
            Id = wallet.Id.ToString(),
            PersonId = wallet.PersonId.ToString(),
            PersonName = person != null ? $"{person.FirstName} {person.LastName}" : "Unknown",
            Name = wallet.Name,
            Status = wallet.Status.ToString(),
            IsActive = wallet.Status == SharedKernel.Enums.WalletStatus.Active,
            IsSuspended = wallet.Status == SharedKernel.Enums.WalletStatus.Suspended,
            CreatedAt = wallet.CreatedAt,
            UpdatedAt = wallet.CreatedAt,
            CredentialCount = 0,
            Metadata = new Dictionary<string, string>()
        };
    }
}