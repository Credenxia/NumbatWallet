using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Queries.Credentials;
using NumbatWallet.Application.Queries.Issuances;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Web.Api.Security;

namespace NumbatWallet.Web.Api.GraphQL.Queries;

/// <summary>
/// GraphQL queries for credential operations
/// </summary>
[ExtendObjectType("Query")]
public class CredentialQuery
{
    private readonly IQueryHandler<GetCredentialByIdQuery, CredentialDto?> _getCredentialByIdHandler;
    private readonly IQueryHandler<GetCredentialsByWalletQuery, IEnumerable<CredentialDto>> _getCredentialsByWalletHandler;
    private readonly IQueryHandler<GetCredentialsByIssuerQuery, IEnumerable<CredentialDto>> _getCredentialsByIssuerHandler;
    private readonly IQueryHandler<SearchCredentialsQuery, PagedResultDto<CredentialDto>> _searchCredentialsHandler;
    private readonly IQueryHandler<GetIssuanceByIdQuery, IssuanceDto?> _getIssuanceByIdHandler;
    private readonly IQueryHandler<GetIssuancesByStatusQuery, IEnumerable<IssuanceDto>> _getIssuancesByStatusHandler;
    private readonly ICredentialRepository? _credentialRepository;
    private readonly IIssuanceRepository? _issuanceRepository;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<CredentialQuery> _logger;

    public CredentialQuery(
        IQueryHandler<GetCredentialByIdQuery, CredentialDto?> getCredentialByIdHandler,
        IQueryHandler<GetCredentialsByWalletQuery, IEnumerable<CredentialDto>> getCredentialsByWalletHandler,
        IQueryHandler<GetCredentialsByIssuerQuery, IEnumerable<CredentialDto>> getCredentialsByIssuerHandler,
        IQueryHandler<SearchCredentialsQuery, PagedResultDto<CredentialDto>> searchCredentialsHandler,
        IQueryHandler<GetIssuanceByIdQuery, IssuanceDto?> getIssuanceByIdHandler,
        IQueryHandler<GetIssuancesByStatusQuery, IEnumerable<IssuanceDto>> getIssuancesByStatusHandler,
        ISecurityAuditService auditService,
        ILogger<CredentialQuery> logger,
        ICredentialRepository? credentialRepository = null,
        IIssuanceRepository? issuanceRepository = null)
    {
        _getCredentialByIdHandler = getCredentialByIdHandler;
        _getCredentialsByWalletHandler = getCredentialsByWalletHandler;
        _getCredentialsByIssuerHandler = getCredentialsByIssuerHandler;
        _searchCredentialsHandler = searchCredentialsHandler;
        _getIssuanceByIdHandler = getIssuanceByIdHandler;
        _getIssuancesByStatusHandler = getIssuancesByStatusHandler;
        _credentialRepository = credentialRepository;
        _issuanceRepository = issuanceRepository;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Get a credential by ID
    /// </summary>
    [GraphQLDescription("Get a credential by its unique identifier")]
    [Authorize]
    public async Task<CredentialDto?> GetCredential(
        string id,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;

        _logger.LogInformation("Fetching credential {CredentialId}", id);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataAccess,
                $"Credential access: {id}");
        }

        var query = new GetCredentialByIdQuery(Guid.Empty, Guid.Parse(id)); // TenantId would come from context
        var credential = await _getCredentialByIdHandler.HandleAsync(query);
        return credential;
    }

    /// <summary>
    /// Get all credentials for a wallet
    /// </summary>
    [GraphQLDescription("Get all credentials associated with a specific wallet")]
    [Authorize]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<CredentialDto>> GetCredentialsByWallet(
        Guid walletId,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;

        _logger.LogInformation("Fetching credentials for wallet {WalletId}", walletId);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataAccess,
                $"Wallet credentials access: {walletId}");
        }

        var query = new GetCredentialsByWalletQuery(Guid.Empty, walletId, false); // TenantId would come from context
        var credentials = await _getCredentialsByWalletHandler.HandleAsync(query);
        return credentials.AsQueryable();
    }

    /// <summary>
    /// Get credentials by issuer
    /// </summary>
    [GraphQLDescription("Get all credentials issued by a specific issuer")]
    [Authorize(Roles = new[] { "Issuer", "Admin" })]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<CredentialDto>> GetCredentialsByIssuer(
        string issuerId,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;

        _logger.LogInformation("Fetching credentials issued by {IssuerId}", issuerId);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataAccess,
                $"Issuer credentials access: {issuerId}");
        }

        var query = new GetCredentialsByIssuerQuery { IssuerId = Guid.Parse(issuerId) };
        var credentials = await _getCredentialsByIssuerHandler.HandleAsync(query);
        return credentials.AsQueryable();
    }

    /// <summary>
    /// Get credentials by type
    /// </summary>
    [GraphQLDescription("Get all credentials of a specific type")]
    [Authorize]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<CredentialDto>> GetCredentialsByType(string type)
    {
        _logger.LogInformation("Fetching credentials of type {Type}", type);

        var query = new SearchCredentialsQuery(
            TenantId: Guid.Empty, // TenantId would come from context
            SearchTerm: null,
            CredentialType: type,
            Status: null,
            PageNumber: 1,
            PageSize: 100, // Default page size
            SortBy: null,
            SortDescending: false);

        var result = await _searchCredentialsHandler.HandleAsync(query);
        return result.Items.AsQueryable();
    }

    /// <summary>
    /// Search credentials
    /// </summary>
    [GraphQLDescription("Search credentials based on various criteria")]
    [Authorize]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<CredentialDto>> SearchCredentials(
        string? holderId = null,
        string? issuerId = null,
        string? type = null,
        string? status = null,
        DateTime? issuedAfter = null,
        DateTime? issuedBefore = null,
        bool? includeRevoked = false,
        bool? includeExpired = false)
    {
        _logger.LogInformation("Searching credentials with criteria");

        // Build search query with filters
        var searchTerm = string.IsNullOrEmpty(type) ? null : type;

        var query = new SearchCredentialsQuery(
            TenantId: Guid.Empty, // TenantId would come from context
            SearchTerm: searchTerm,
            CredentialType: type,
            Status: status,
            PageNumber: 1,
            PageSize: 1000, // Large page size for GraphQL - paging handled by HotChocolate
            SortBy: null,
            SortDescending: false);

        var result = await _searchCredentialsHandler.HandleAsync(query);
        return result.Items.AsQueryable();
    }

    /// <summary>
    /// Get an issuance by ID
    /// </summary>
    [GraphQLDescription("Get an issuance request by its unique identifier")]
    [Authorize]
    public async Task<IssuanceDto?> GetIssuance(
        Guid id,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;

        _logger.LogInformation("Fetching issuance {IssuanceId}", id);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataAccess,
                $"Issuance access: {id}");
        }

        var query = new GetIssuanceByIdQuery(id);
        var issuance = await _getIssuanceByIdHandler.HandleAsync(query);
        return issuance;
    }

    /// <summary>
    /// Get issuances by status
    /// </summary>
    [GraphQLDescription("Get all issuance requests with a specific status")]
    [Authorize(Roles = new[] { "Issuer", "Admin" })]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<IssuanceDto>> GetIssuancesByStatus(string status)
    {
        _logger.LogInformation("Fetching issuances with status {Status}", status);

        var query = new GetIssuancesByStatusQuery(status, null, null, null, null);
        var issuances = await _getIssuancesByStatusHandler.HandleAsync(query);
        return issuances.AsQueryable();
    }

    /// <summary>
    /// Get pending issuances
    /// </summary>
    [GraphQLDescription("Get all pending issuance requests awaiting approval")]
    [Authorize(Roles = new[] { "Issuer", "Admin" })]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<IssuanceDto>> GetPendingIssuances()
    {
        _logger.LogInformation("Fetching pending issuances");

        var query = new GetIssuancesByStatusQuery("Pending", null, null, null, null);
        var issuances = await _getIssuancesByStatusHandler.HandleAsync(query);
        return issuances.AsQueryable();
    }

    /// <summary>
    /// Get issuances for a wallet
    /// </summary>
    [GraphQLDescription("Get all issuance requests for a specific wallet")]
    [Authorize]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<IssuanceDto>> GetIssuancesByWallet(Guid walletId)
    {
        _logger.LogInformation("Fetching issuances for wallet {WalletId}", walletId);

        // Use repository directly since we don't have a specific query handler for this
        if (_issuanceRepository == null)
        {
            _logger.LogWarning("IssuanceRepository not available, returning empty result");
            return new List<IssuanceDto>().AsQueryable();
        }

        var issuances = await _issuanceRepository.GetByWalletIdAsync(walletId);

        // Map to DTOs
        var issuanceDtos = issuances.Select(i => new IssuanceDto
        {
            Id = i.Id,
            WalletId = i.WalletId,
            RequesterId = i.RequesterId,
            CredentialType = i.CredentialType,
            Status = i.Status,
            CreatedAt = i.CreatedAt,
            ApprovedAt = i.ApprovedAt,
            RejectedAt = i.RejectedAt,
            CompletedAt = i.CompletedAt,
            ApprovedBy = i.ApprovedBy,
            RejectedBy = i.RejectedBy,
            CompletedBy = i.CompletedBy,
            RejectionReason = i.RejectionReason,
            CredentialId = i.CredentialId,
            CredentialData = i.Claims.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        }).ToList();

        return issuanceDtos.AsQueryable();
    }

    private static TimeSpan CalculateAverageValidityPeriod(List<CredentialDto> credentials)
    {
        var validCredentials = credentials.Where(c => c.IssuanceDate != default && c.ExpirationDate.HasValue);
        if (!validCredentials.Any())
        {
            return TimeSpan.Zero;
        }

        var totalDays = validCredentials.Average(c => (c.ExpirationDate!.Value - c.IssuanceDate).TotalDays);
        return TimeSpan.FromDays(totalDays);
    }

    /// <summary>
    /// Get credential statistics
    /// </summary>
    [GraphQLDescription("Get statistical information about credentials")]
    [Authorize(Roles = new[] { "Admin" })]
    public async Task<CredentialStatistics> GetCredentialStatistics(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        _logger.LogInformation("Fetching credential statistics");

        // Calculate actual statistics from the database
        var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTime.UtcNow;
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddMonths(-1);

        // Fetch real credentials from database using SearchCredentialsQuery
        var query = new SearchCredentialsQuery(
            TenantId: Guid.Empty, // TenantId would come from context
            SearchTerm: null,
            CredentialType: null,
            Status: null,
            PageNumber: 1,
            PageSize: 10000, // Large page size to get all credentials for statistics
            SortBy: null,
            SortDescending: false);

        var searchResult = await _searchCredentialsHandler.HandleAsync(query);
        var allCredentials = searchResult.Items.ToList();

        // Calculate statistics from real data
        var stats = new CredentialStatistics
        {
            TotalCredentials = allCredentials.Count,
            ActiveCredentials = allCredentials.Count(c => c.Status == "Active"),
            RevokedCredentials = allCredentials.Count(c => c.IsRevoked),
            ExpiredCredentials = allCredentials.Count(c => c.ExpirationDate.HasValue && c.ExpirationDate < DateTime.UtcNow),
            IssuedToday = allCredentials.Count(c => c.IssuanceDate.Date == today),
            IssuedThisWeek = allCredentials.Count(c => c.IssuanceDate >= weekAgo),
            IssuedThisMonth = allCredentials.Count(c => c.IssuanceDate >= monthAgo),
            CredentialsByType = allCredentials.GroupBy(c => c.Type ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count()),
            CredentialsByIssuer = allCredentials.GroupBy(c => c.IssuerId ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count()),
            AverageValidityPeriod = allCredentials.Any() ? CalculateAverageValidityPeriod(allCredentials) : TimeSpan.Zero
        };

        return stats;
    }

    /// <summary>
    /// Get issuance statistics
    /// </summary>
    [GraphQLDescription("Get statistical information about issuances")]
    [Authorize(Roles = new[] { "Admin" })]
    public async Task<IssuanceProcessStatistics> GetIssuanceStatistics(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        _logger.LogInformation("Fetching issuance statistics");

        // Calculate actual statistics from the database
        var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTime.UtcNow;

        // Fetch issuances by different statuses
        var allIssuancesQuery = new GetIssuancesByStatusQuery(null, start, end, null, null);
        var allIssuances = (await _getIssuancesByStatusHandler.HandleAsync(allIssuancesQuery)).ToList();

        var pendingQuery = new GetIssuancesByStatusQuery("Pending", start, end, null, null);
        var pendingIssuances = (await _getIssuancesByStatusHandler.HandleAsync(pendingQuery)).ToList();

        var approvedQuery = new GetIssuancesByStatusQuery("Approved", start, end, null, null);
        var approvedIssuances = (await _getIssuancesByStatusHandler.HandleAsync(approvedQuery)).ToList();

        var rejectedQuery = new GetIssuancesByStatusQuery("Rejected", start, end, null, null);
        var rejectedIssuances = (await _getIssuancesByStatusHandler.HandleAsync(rejectedQuery)).ToList();

        var completedQuery = new GetIssuancesByStatusQuery("Completed", start, end, null, null);
        var completedIssuances = (await _getIssuancesByStatusHandler.HandleAsync(completedQuery)).ToList();

        // Calculate average processing time
        var processedIssuances = allIssuances.Where(i => i.CompletedAt.HasValue && i.CreatedAt != default);
        var averageProcessingTime = processedIssuances.Any()
            ? TimeSpan.FromMinutes(processedIssuances.Average(i => (i.CompletedAt!.Value - i.CreatedAt).TotalMinutes))
            : TimeSpan.Zero;

        var stats = new IssuanceProcessStatistics
        {
            TotalIssuances = allIssuances.Count,
            PendingIssuances = pendingIssuances.Count,
            ApprovedIssuances = approvedIssuances.Count,
            RejectedIssuances = rejectedIssuances.Count,
            CompletedIssuances = completedIssuances.Count,
            AverageProcessingTime = averageProcessingTime,
            IssuancesByType = allIssuances.GroupBy(i => i.CredentialType ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count()),
            IssuancesByStatus = allIssuances.GroupBy(i => i.Status ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return stats;
    }
}

/// <summary>
/// Credential statistics model
/// </summary>
public class CredentialStatistics
{
    public int TotalCredentials { get; set; }
    public int ActiveCredentials { get; set; }
    public int RevokedCredentials { get; set; }
    public int ExpiredCredentials { get; set; }
    public int IssuedToday { get; set; }
    public int IssuedThisWeek { get; set; }
    public int IssuedThisMonth { get; set; }
    public Dictionary<string, int> CredentialsByType { get; set; } = new();
    public Dictionary<string, int> CredentialsByIssuer { get; set; } = new();
    public TimeSpan AverageValidityPeriod { get; set; }
}

/// <summary>
/// Issuance statistics model
/// </summary>
public class IssuanceProcessStatistics
{
    public int TotalIssuances { get; set; }
    public int PendingIssuances { get; set; }
    public int ApprovedIssuances { get; set; }
    public int RejectedIssuances { get; set; }
    public int CompletedIssuances { get; set; }
    public TimeSpan AverageProcessingTime { get; set; }
    public Dictionary<string, int> IssuancesByType { get; set; } = new();
    public Dictionary<string, int> IssuancesByStatus { get; set; } = new();
}