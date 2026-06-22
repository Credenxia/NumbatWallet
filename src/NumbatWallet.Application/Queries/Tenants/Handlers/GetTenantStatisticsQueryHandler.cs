using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Application.Queries.Tenants.Handlers;

public class GetTenantStatisticsQueryHandler : IQueryHandler<GetTenantStatisticsQuery, TenantStatisticsDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly IPersonRepository _personRepository;
    private readonly ILogger<GetTenantStatisticsQueryHandler> _logger;

    public GetTenantStatisticsQueryHandler(
        ITenantRepository tenantRepository,
        IWalletRepository walletRepository,
        ICredentialRepository credentialRepository,
        IPersonRepository personRepository,
        ILogger<GetTenantStatisticsQueryHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _walletRepository = walletRepository;
        _credentialRepository = credentialRepository;
        _personRepository = personRepository;
        _logger = logger;
    }

    public async Task<TenantStatisticsDto> HandleAsync(GetTenantStatisticsQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching statistics for tenant {TenantId}", query.TenantId);

        // Verify tenant exists
        var tenant = await _tenantRepository.GetByIdAsync(query.TenantId, cancellationToken);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant {query.TenantId} not found");
        }

        // Get all persons for this tenant
        var allPersons = await _personRepository.GetAllAsync(cancellationToken);
        var tenantPersons = allPersons.Where(p => p.TenantId == query.TenantId.ToString()).ToList();
        var userCount = tenantPersons.Count;

        // Get all wallets for this tenant's persons
        var allWallets = await _walletRepository.GetAllAsync(cancellationToken);
        var personIds = tenantPersons.Select(p => p.Id).ToHashSet();
        var tenantWallets = allWallets.Where(w => personIds.Contains(w.PersonId)).ToList();
        var walletCount = tenantWallets.Count;

        // Get all credentials for this tenant's wallets
        var allCredentials = await _credentialRepository.GetAllAsync(cancellationToken);
        var walletIds = tenantWallets.Select(w => w.Id).ToHashSet();
        var tenantCredentials = allCredentials.Where(c => walletIds.Contains(c.WalletId)).ToList();
        var credentialCount = tenantCredentials.Count;

        // Calculate storage (simplified - in production would query actual storage)
        var storageUsedMB = (decimal)(credentialCount * 0.1 + walletCount * 0.05 + userCount * 0.2);

        // Get tenant name
        var tenantName = tenant.Name;

        var statistics = new TenantStatisticsDto
        {
            TenantId = query.TenantId,
            TenantName = tenantName,
            TotalUsers = userCount,
            TotalWallets = walletCount,
            TotalCredentials = credentialCount,
            StorageUsedGB = Math.Round(storageUsedMB / 1024, 2), // Convert MB to GB
            ComputeHoursUsed = 0, // Would need compute tracking service
            PeriodStart = DateTime.UtcNow.AddMonths(-1),
            PeriodEnd = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Statistics for tenant {TenantId}: {UserCount} users, {WalletCount} wallets, {CredentialCount} credentials",
            query.TenantId, userCount, walletCount, credentialCount);

        return statistics;
    }
}