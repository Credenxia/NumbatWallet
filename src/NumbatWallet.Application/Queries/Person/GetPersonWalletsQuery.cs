using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Queries.Person;

/// <summary>
/// Query for retrieving all wallets belonging to a person
/// POA: Person-specific wallet retrieval
/// </summary>
public sealed record GetPersonWalletsQuery : IQuery<IEnumerable<WalletDto>>
{
    public Guid PersonId { get; init; }
}

/// <summary>
/// Handler for retrieving all wallets for a specific person
/// POA: Implementation for person wallet relationship
/// </summary>
public sealed class GetPersonWalletsQueryHandler : IQueryHandler<GetPersonWalletsQuery, IEnumerable<WalletDto>>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly ILogger<GetPersonWalletsQueryHandler> _logger;

    public GetPersonWalletsQueryHandler(
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        ILogger<GetPersonWalletsQueryHandler> logger)
    {
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<WalletDto>> HandleAsync(
        GetPersonWalletsQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving wallets for person {PersonId}", query.PersonId);

        // Verify person exists
        var person = await _personRepository.GetByIdAsync(query.PersonId, cancellationToken);
        if (person == null)
        {
            _logger.LogWarning("Person {PersonId} not found", query.PersonId);
            throw new EntityNotFoundException("Person", query.PersonId.ToString());
        }

        // Get all wallets for this person
        var wallets = await _walletRepository.GetByPersonIdAsync(query.PersonId, cancellationToken);

        var walletDtos = wallets.Select(wallet => new WalletDto
        {
            Id = wallet.Id.ToString(),
            PersonId = wallet.PersonId.ToString(),
            PersonName = $"{person.FirstName} {person.LastName}",
            Name = wallet.Name,
            Status = wallet.Status.ToString(),
            IsActive = wallet.Status == SharedKernel.Enums.WalletStatus.Active,
            IsSuspended = wallet.Status == SharedKernel.Enums.WalletStatus.Suspended,
            CreatedAt = wallet.CreatedAt,
            UpdatedAt = wallet.ModifiedAt ?? wallet.CreatedAt,
            CredentialCount = wallet.GetCredentialCount(),
            Metadata = new Dictionary<string, string>
            {
                ["Type"] = wallet.Type.ToString(),
                ["DID"] = wallet.WalletDid,
                ["TenantId"] = wallet.TenantId
            }
        }).ToList();

        _logger.LogInformation("Retrieved {Count} wallets for person {PersonId}",
            walletDtos.Count, query.PersonId);

        return walletDtos;
    }
}