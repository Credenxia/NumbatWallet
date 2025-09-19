using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Repositories;
using NumbatWallet.Domain.Specifications;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Wallets;

public record CreateWalletCommand : ICommand<WalletDto>
{
    public required string PersonId { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

public class CreateWalletCommandHandler : ICommandHandler<CreateWalletCommand, WalletDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly ILogger<CreateWalletCommandHandler> _logger;

    public CreateWalletCommandHandler(
        IUnitOfWork unitOfWork,
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        ILogger<CreateWalletCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _logger = logger;
    }

    public async Task<WalletDto> HandleAsync(
        CreateWalletCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating wallet for person {PersonId}", command.PersonId);

        // Validate person exists
        var personId = Guid.Parse(command.PersonId);
        var person = await _personRepository.GetByIdAsync(personId, cancellationToken);
        if (person == null)
        {
            throw new EntityNotFoundException("Person", command.PersonId);
        }

        // Check if person already has an active wallet
        var existingWallets = await _walletRepository.FindAsync(
            new Domain.Specifications.WalletByPersonSpecification(personId),
            cancellationToken);

        if (existingWallets.Any(w => w.Status == NumbatWallet.SharedKernel.Enums.WalletStatus.Active))
        {
            throw new DomainValidationException($"Person {command.PersonId} already has an active wallet");
        }

        // Create wallet
        var walletResult = Wallet.Create(personId, command.Name);
        if (walletResult.IsFailure)
        {
            throw new DomainValidationException(walletResult.Error.Message);
        }

        var wallet = walletResult.Value;

        // Wallet is created as Active by default, no need to activate

        // Save wallet
        await _walletRepository.AddAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Wallet {WalletId} created successfully for person {PersonId}",
            wallet.Id, command.PersonId);

        // Map to DTO
        return MapToDto(wallet, person);
    }

    private static WalletDto MapToDto(Wallet wallet, Domain.Aggregates.Person person)
    {
        return new WalletDto
        {
            Id = wallet.Id.ToString(),
            PersonId = wallet.PersonId.ToString(),
            PersonName = $"{person.FirstName} {person.LastName}",
            Name = wallet.Name,
            Status = wallet.Status.ToString(),
            IsActive = wallet.Status == NumbatWallet.SharedKernel.Enums.WalletStatus.Active,
            IsSuspended = wallet.Status == NumbatWallet.SharedKernel.Enums.WalletStatus.Suspended,
            CreatedAt = wallet.CreatedAt,
            UpdatedAt = wallet.CreatedAt,
            CredentialCount = 0, // Would need to query credentials
            Metadata = new Dictionary<string, string>()
        };
    }
}