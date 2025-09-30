using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.Domain.Specifications;

namespace NumbatWallet.Application.Queries.Wallets;

public class GetWalletsByPersonQueryHandler : IQueryHandler<GetWalletsByPersonQuery, IEnumerable<WalletDto>>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly ICurrentTenantService _tenantService;

    public GetWalletsByPersonQueryHandler(
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        ICredentialRepository credentialRepository,
        ICurrentTenantService tenantService)
    {
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _credentialRepository = credentialRepository;
        _tenantService = tenantService;
    }

    public async Task<IEnumerable<WalletDto>> HandleAsync(
        GetWalletsByPersonQuery query,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.TenantId;

        // Get wallets for the person using specification
        var walletSpec = new WalletByPersonSpecification(query.PersonId);
        var wallets = await _walletRepository.FindAsync(walletSpec, cancellationToken);

        // Filter by tenant and inactive status
        wallets = wallets.Where(w => w.TenantId == tenantId).ToList();
        if (!query.IncludeInactive)
        {
            wallets = wallets.Where(w => w.Status == SharedKernel.Enums.WalletStatus.Active).ToList();
        }

        // Get person details once
        var person = await _personRepository.GetByIdAsync(query.PersonId, cancellationToken);
        var personName = person != null ? $"{person.FirstName} {person.LastName}" : "Unknown";

        // Convert to DTOs
        var walletDtos = new List<WalletDto>();
        foreach (var wallet in wallets.OrderByDescending(w => w.CreatedAt))
        {
            // Get credential count for each wallet
            var credentialSpec = new CredentialByWalletSpecification(wallet.Id);
            var credentials = await _credentialRepository.FindAsync(credentialSpec, cancellationToken);

            walletDtos.Add(new WalletDto
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
                CredentialCount = credentials.Count(),
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = wallet.Type.ToString(),
                    ["DID"] = wallet.WalletDid,
                    ["TenantId"] = wallet.TenantId
                }
            });
        }

        return walletDtos;
    }
}