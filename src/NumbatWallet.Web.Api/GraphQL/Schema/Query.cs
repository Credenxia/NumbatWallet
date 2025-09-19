using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using HotChocolate.Types;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.ValueObjects;

namespace NumbatWallet.Web.Api.GraphQL.Schema;

public class Query
{
    private readonly IPersonService _personService;
    private readonly IOrganizationService _organizationService;
    private readonly ICredentialService _credentialService;
    private readonly IWalletService _walletService;

    public Query(
        IPersonService personService,
        IOrganizationService organizationService,
        ICredentialService credentialService,
        IWalletService walletService)
    {
        _personService = personService;
        _organizationService = organizationService;
        _credentialService = credentialService;
        _walletService = walletService;
    }

    // Person Queries
    [Authorize]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<PersonDto>> GetPersons(
        [Service] IPersonService personService,
        CancellationToken cancellationToken)
    {
        var persons = await personService.GetAllAsync(cancellationToken);
        return persons.AsQueryable();
    }

    [Authorize]
    public async Task<PersonDto?> GetPersonById(
        Guid id,
        [Service] IPersonService personService,
        CancellationToken cancellationToken)
    {
        return await personService.GetByIdAsync(id, cancellationToken);
    }

    [Authorize]
    public async Task<PersonDto?> GetPersonByEmail(
        string email,
        [Service] IPersonService personService,
        CancellationToken cancellationToken)
    {
        return await personService.GetByEmailAsync(email, cancellationToken);
    }

    // Organization Queries
    [Authorize(Roles = new[] { "Admin", "Officer" })]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<OrganizationDto>> GetOrganizations(
        [Service] IOrganizationService organizationService,
        CancellationToken cancellationToken)
    {
        var organizations = await organizationService.GetAllAsync(cancellationToken);
        return organizations.AsQueryable();
    }

    [Authorize]
    public async Task<OrganizationDto?> GetOrganizationById(
        Guid id,
        [Service] IOrganizationService organizationService,
        CancellationToken cancellationToken)
    {
        return await organizationService.GetByIdAsync(id, cancellationToken);
    }

    // Wallet Queries
    [Authorize]
    public async Task<WalletDto?> GetWalletById(
        Guid id,
        [Service] IWalletService walletService,
        CancellationToken cancellationToken)
    {
        return await walletService.GetByIdAsync(id, cancellationToken);
    }

    [Authorize]
    public async Task<WalletDto?> GetWalletByPersonId(
        Guid personId,
        [Service] IWalletService walletService,
        CancellationToken cancellationToken)
    {
        return await walletService.GetByPersonIdAsync(personId, cancellationToken);
    }

    [Authorize]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<WalletDto>> GetMyWallets(
        [Service] IWalletService walletService,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Enumerable.Empty<WalletDto>().AsQueryable();

        var wallets = await walletService.GetByUserIdAsync(userId, cancellationToken);
        return wallets.AsQueryable();
    }

    // Credential Queries
    [Authorize]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<CredentialDto>> GetCredentials(
        Guid walletId,
        [Service] ICredentialService credentialService,
        CancellationToken cancellationToken)
    {
        var credentials = await credentialService.GetByWalletIdAsync(walletId, cancellationToken);
        return credentials.AsQueryable();
    }

    [Authorize]
    public async Task<CredentialDto?> GetCredentialById(
        Guid id,
        [Service] ICredentialService credentialService,
        CancellationToken cancellationToken)
    {
        return await credentialService.GetByIdAsync(id, cancellationToken);
    }

    [Authorize]
    public async Task<IEnumerable<CredentialDto>> GetActiveCredentials(
        Guid walletId,
        [Service] ICredentialService credentialService,
        CancellationToken cancellationToken)
    {
        return await credentialService.GetActiveCredentialsAsync(walletId, cancellationToken);
    }

    [Authorize]
    public async Task<IEnumerable<CredentialDto>> GetExpiredCredentials(
        Guid walletId,
        [Service] ICredentialService credentialService,
        CancellationToken cancellationToken)
    {
        return await credentialService.GetExpiredCredentialsAsync(walletId, cancellationToken);
    }

    // Statistics Queries
    [Authorize(Roles = new[] { "Admin", "Officer" })]
    public async Task<DashboardStatistics> GetDashboardStatistics(
        [Service] IStatisticsService statisticsService,
        CancellationToken cancellationToken)
    {
        return await statisticsService.GetDashboardStatisticsAsync(cancellationToken);
    }

    [Authorize(Roles = new[] { "Admin", "Officer" })]
    public async Task<IEnumerable<IssuanceStatistics>> GetIssuanceStatistics(
        DateTime startDate,
        DateTime endDate,
        [Service] IStatisticsService statisticsService,
        CancellationToken cancellationToken)
    {
        return await statisticsService.GetIssuanceStatisticsAsync(startDate, endDate, cancellationToken);
    }

    // Health Check Query
    [AllowAnonymous]
    public async Task<HealthStatus> GetHealthStatus(
        [Service] IHealthCheckService healthCheckService,
        CancellationToken cancellationToken)
    {
        return await healthCheckService.GetHealthStatusAsync(cancellationToken);
    }
}

// Supporting Types
public class DashboardStatistics
{
    public int TotalUsers { get; set; }
    public int ActiveWallets { get; set; }
    public int TotalCredentials { get; set; }
    public int CredentialsIssuedToday { get; set; }
    public int CredentialsExpiringThisWeek { get; set; }
    public Dictionary<string, int> CredentialsByType { get; set; } = new();
}

public class IssuanceStatistics
{
    public DateTime Date { get; set; }
    public int IssuedCount { get; set; }
    public int RevokedCount { get; set; }
    public int ExpiredCount { get; set; }
    public Dictionary<string, int> ByCredentialType { get; set; } = new();
}

public class HealthStatus
{
    public string Status { get; set; } = "Healthy";
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

public class ComponentHealth
{
    public string Status { get; set; } = "Healthy";
    public string? Description { get; set; }
    public TimeSpan ResponseTime { get; set; }
}