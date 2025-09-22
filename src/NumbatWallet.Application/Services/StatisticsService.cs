using Microsoft.EntityFrameworkCore;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IPersonRepository _personRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly IOrganizationRepository _organizationRepository;

    public StatisticsService(
        IPersonRepository personRepository,
        IWalletRepository walletRepository,
        ICredentialRepository credentialRepository,
        IOrganizationRepository organizationRepository)
    {
        _personRepository = personRepository;
        _walletRepository = walletRepository;
        _credentialRepository = credentialRepository;
        _organizationRepository = organizationRepository;
    }

    public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new DashboardStatisticsDto();

        // Get total counts
        var allPersons = await _personRepository.GetAllAsync(cancellationToken);
        stats.TotalPersons = allPersons.Count();

        var allWallets = await _walletRepository.GetAllAsync(cancellationToken);
        stats.TotalWallets = allWallets.Count();
        stats.ActiveWallets = allWallets.Count(w => w.Status == WalletStatus.Active);

        var allCredentials = await _credentialRepository.GetAllAsync(cancellationToken);
        stats.TotalCredentials = allCredentials.Count();
        stats.ActiveCredentials = allCredentials.Count(c => c.Status == CredentialStatus.Active);

        // Today's issuances
        var today = DateTime.UtcNow.Date;
        stats.CredentialsIssuedToday = allCredentials.Count(c => c.IssuedAt.Date == today);

        // Expiring this week
        var weekFromNow = DateTime.UtcNow.AddDays(7);
        stats.CredentialsExpiringThisWeek = allCredentials.Count(
            c => c.ExpiresAt != null &&
                 c.ExpiresAt > DateTime.UtcNow &&
                 c.ExpiresAt <= weekFromNow
        );

        // Group by schema (type)
        stats.CredentialsByType = allCredentials
            .GroupBy(c => c.SchemaId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group wallets by status
        stats.WalletsByStatus = allWallets
            .GroupBy(w => w.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return stats;
    }

    public async Task<IEnumerable<IssuanceStatisticsDto>> GetIssuanceStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var allCredentials = await _credentialRepository.GetAllAsync(cancellationToken);

        var statistics = new List<IssuanceStatisticsDto>();

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var dayStats = new IssuanceStatisticsDto
            {
                Date = date,
                IssuedCount = allCredentials.Count(c => c.IssuedAt.Date == date),
                RevokedCount = allCredentials.Count(c => c.RevokedAt?.Date == date),
                ExpiredCount = allCredentials.Count(c => c.ExpiresAt?.Date == date)
            };

            dayStats.ByCredentialType = allCredentials
                .Where(c => c.IssuedAt.Date == date)
                .GroupBy(c => c.CredentialType)
                .ToDictionary(g => g.Key, g => g.Count());

            statistics.Add(dayStats);
        }

        return statistics;
    }

    public async Task<TenantStatisticsDto> GetTenantStatisticsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // For POA, we're using single-tenant mode
        // In production, this would filter by tenant
        var stats = new TenantStatisticsDto
        {
            TenantId = tenantId,
            TenantName = "Default Tenant"
        };

        var allPersons = await _personRepository.GetAllAsync(cancellationToken);
        stats.TotalUsers = allPersons.Count();

        var allWallets = await _walletRepository.GetAllAsync(cancellationToken);
        stats.TotalWallets = allWallets.Count();

        var allCredentials = await _credentialRepository.GetAllAsync(cancellationToken);
        stats.TotalCredentials = allCredentials.Count();

        // Mock storage and compute metrics
        stats.StorageUsedGB = Math.Round((decimal)(stats.TotalCredentials * 0.001), 3);
        stats.ComputeHoursUsed = Math.Round((decimal)(stats.TotalCredentials * 0.01), 2);

        stats.PeriodStart = DateTime.UtcNow.AddMonths(-1);
        stats.PeriodEnd = DateTime.UtcNow;

        return stats;
    }

    public async Task<SystemMetricsDto> GetSystemMetricsAsync(CancellationToken cancellationToken = default)
    {
        // Mock system metrics for POA
        // In production, these would come from actual monitoring
        var random = new Random();

        return new SystemMetricsDto
        {
            CpuUsagePercent = Math.Round(random.NextDouble() * 30 + 10, 2), // 10-40%
            MemoryUsagePercent = Math.Round(random.NextDouble() * 40 + 30, 2), // 30-70%
            DatabaseConnectionsActive = random.Next(5, 20),
            CacheHitRatio = random.Next(85, 99),
            AverageResponseTimeMs = Math.Round(random.NextDouble() * 100 + 50, 2), // 50-150ms
            RequestsPerSecond = random.Next(10, 100),
            ErrorCounts = new Dictionary<string, long>
            {
                { "4xx", random.Next(0, 5) },
                { "5xx", random.Next(0, 2) }
            }
        };
    }
}
