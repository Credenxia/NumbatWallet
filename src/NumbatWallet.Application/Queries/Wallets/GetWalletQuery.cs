using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Queries.Wallets;

public record GetWalletQuery : IQuery<WalletDto>
{
    public required string WalletId { get; init; }
    public bool IncludeCredentials { get; init; } = false;
}

public class GetWalletQueryHandler : IQueryHandler<GetWalletQuery, WalletDto>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly ILogger<GetWalletQueryHandler> _logger;

    public GetWalletQueryHandler(
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        ICredentialRepository credentialRepository,
        ILogger<GetWalletQueryHandler> logger)
    {
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _credentialRepository = credentialRepository;
        _logger = logger;
    }

    public async Task<WalletDto> HandleAsync(
        GetWalletQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting wallet {WalletId}", query.WalletId);

        var walletId = Guid.Parse(query.WalletId);
        var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken);

        if (wallet == null)
        {
            throw new EntityNotFoundException("Wallet", query.WalletId);
        }

        // Get person details
        var person = await _personRepository.GetByIdAsync(wallet.PersonId, cancellationToken);

        // Get credential count if requested
        var credentialCount = 0;
        if (query.IncludeCredentials)
        {
            var credentials = await _credentialRepository.FindAsync(
                new Domain.Specifications.CredentialByWalletSpecification(walletId),
                cancellationToken);
            credentialCount = credentials.Count();
        }

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
            CredentialCount = credentialCount,
            Metadata = new Dictionary<string, string>()
        };
    }
}