using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Queries.Wallets;

/// <summary>
/// Alternative handler for simple wallet list queries
/// POA: Real implementation for wallet queries without pagination
/// </summary>
public sealed class GetAllWalletsQueryHandler : IQueryHandler<GetAllWalletsQuery, IEnumerable<WalletDto>>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly ILogger<GetAllWalletsQueryHandler> _logger;

    public GetAllWalletsQueryHandler(
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        ILogger<GetAllWalletsQueryHandler> logger)
    {
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<WalletDto>> HandleAsync(
        GetAllWalletsQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving wallets with filter: PersonId={PersonId}, Status={Status}",
            query.PersonId, query.Status);

        // Get wallets based on query parameters
        var wallets = query.PersonId.HasValue
            ? await _walletRepository.GetByPersonIdAsync(query.PersonId.Value, cancellationToken)
            : await _walletRepository.GetAllAsync(cancellationToken);

        // Filter by status if specified
        if (!string.IsNullOrEmpty(query.Status))
        {
            wallets = wallets.Where(w => w.Status.ToString().Equals(query.Status, StringComparison.OrdinalIgnoreCase));
        }

        // Map to DTOs with person information
        var walletDtos = new List<WalletDto>();
        foreach (var wallet in wallets)
        {
            var person = await _personRepository.GetByIdAsync(wallet.PersonId, cancellationToken);

            walletDtos.Add(new WalletDto
            {
                Id = wallet.Id.ToString(),
                PersonId = wallet.PersonId.ToString(),
                PersonName = person != null ? $"{person.FirstName} {person.LastName}" : "Unknown",
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
                    ["DID"] = wallet.WalletDid
                }
            });
        }

        _logger.LogInformation("Retrieved {Count} wallets", walletDtos.Count);
        return walletDtos;
    }
}

/// <summary>
/// Handler for retrieving wallets for a specific user
/// </summary>
public sealed class GetMyWalletsQueryHandler : IQueryHandler<GetMyWalletsQuery, IEnumerable<WalletDto>>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly ILogger<GetMyWalletsQueryHandler> _logger;

    public GetMyWalletsQueryHandler(
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        ILogger<GetMyWalletsQueryHandler> logger)
    {
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<WalletDto>> HandleAsync(
        GetMyWalletsQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving wallets for user {UserId}", query.UserId);

        // For POA demo, we'll get all persons and filter by external ID (UserId)
        // Note: In production, this should be properly filtered at repository level
        var allPersons = await _personRepository.GetAllAsync(cancellationToken);
        var persons = allPersons.Where(p => p.ExternalId == query.UserId).ToList();

        var walletDtos = new List<WalletDto>();
        foreach (var person in persons)
        {
            var wallets = await _walletRepository.GetByPersonIdAsync(person.Id, cancellationToken);

            foreach (var wallet in wallets)
            {
                walletDtos.Add(new WalletDto
                {
                    Id = wallet.Id.ToString(),
                    PersonId = person.Id.ToString(),
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
                });
            }
        }

        _logger.LogInformation("Retrieved {Count} wallets for user {UserId}", walletDtos.Count, query.UserId);
        return walletDtos;
    }
}