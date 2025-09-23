using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Queries.Wallets;

/// <summary>
/// Handler for retrieving a wallet by ID
/// POA: Real implementation with full wallet details
/// </summary>
public sealed class GetWalletByIdQueryHandler : IQueryHandler<GetWalletByIdQuery, WalletDto>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly ILogger<GetWalletByIdQueryHandler> _logger;

    public GetWalletByIdQueryHandler(
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        ICredentialRepository credentialRepository,
        ILogger<GetWalletByIdQueryHandler> logger)
    {
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _credentialRepository = credentialRepository;
        _logger = logger;
    }

    public async Task<WalletDto> HandleAsync(
        GetWalletByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving wallet {WalletId}", query.WalletId);

        var wallet = await _walletRepository.GetByIdAsync(query.WalletId, cancellationToken);
        if (wallet == null)
        {
            _logger.LogWarning("Wallet {WalletId} not found", query.WalletId);
            throw new EntityNotFoundException("Wallet", query.WalletId.ToString());
        }

        // Get person details
        var person = await _personRepository.GetByIdAsync(wallet.PersonId, cancellationToken);
        if (person == null)
        {
            _logger.LogWarning("Person {PersonId} not found for wallet {WalletId}",
                wallet.PersonId, query.WalletId);
        }

        // Get credential count
        var credentials = await _credentialRepository.FindAsync(
            new Domain.Specifications.CredentialByWalletSpecification(query.WalletId),
            cancellationToken);

        var activeCredentials = credentials.Count(c => c.IsActive());
        var expiredCredentials = credentials.Count(c => c.IsExpired());

        var walletDto = new WalletDto
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
            CredentialCount = credentials.Count(),
            Metadata = new Dictionary<string, string>
            {
                ["Type"] = wallet.Type.ToString(),
                ["DID"] = wallet.WalletDid,
                ["TenantId"] = wallet.TenantId,
                ["ActiveCredentials"] = activeCredentials.ToString(),
                ["ExpiredCredentials"] = expiredCredentials.ToString(),
                ["SuspensionReason"] = wallet.SuspensionReason ?? string.Empty,
                ["LockReason"] = wallet.LockReason ?? string.Empty
            }
        };

        _logger.LogInformation("Retrieved wallet {WalletId} with {CredentialCount} credentials",
            query.WalletId, walletDto.CredentialCount);

        return walletDto;
    }
}