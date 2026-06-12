using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Queries.Presentations;
using NumbatWallet.Web.Api.Security;

namespace NumbatWallet.Web.Api.GraphQL.Queries;

/// <summary>
/// GraphQL queries for credential presentations.
/// Summaries expose disclosed claim NAMES only, never the presented values.
/// </summary>
[ExtendObjectType("Query")]
public class PresentationQuery
{
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<PresentationQuery> _logger;

    // NOTE: query handlers and repositories are SCOPED (they depend on the scoped DbContext)
    // and must be resolved per-resolver via [Service] parameters. HotChocolate object types
    // are singletons, so constructor-injecting a scoped dependency captures a DbContext that
    // is disposed after the first request (captive dependency → ObjectDisposedException).
    // Only singletons (audit service, logger) may live in the constructor.
    public PresentationQuery(
        ISecurityAuditService auditService,
        ILogger<PresentationQuery> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Get a presentation by its unique identifier.
    /// </summary>
    [GraphQLDescription("Get a presentation by its unique identifier")]
    [Authorize]
    public async Task<PresentationSummaryDto?> GetPresentationById(
        Guid id,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] IQueryHandler<GetPresentationByIdQuery, PresentationSummaryDto?> getPresentationByIdHandler)
    {
        var httpContext = httpContextAccessor.HttpContext;

        _logger.LogInformation("Fetching presentation {PresentationId}", id);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataAccess,
                $"Presentation access: {id}");
        }

        return await getPresentationByIdHandler.HandleAsync(new GetPresentationByIdQuery(id));
    }

    /// <summary>
    /// Get all presentations made from a specific wallet.
    /// </summary>
    [GraphQLDescription("Get all presentations made from a specific wallet")]
    [Authorize]
    public async Task<IEnumerable<PresentationSummaryDto>> GetPresentationsByWallet(
        Guid walletId,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] IQueryHandler<GetPresentationsByWalletQuery, IEnumerable<PresentationSummaryDto>> getPresentationsByWalletHandler)
    {
        var httpContext = httpContextAccessor.HttpContext;

        _logger.LogInformation("Fetching presentations for wallet {WalletId}", walletId);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataAccess,
                $"Wallet presentations access: {walletId}");
        }

        return await getPresentationsByWalletHandler.HandleAsync(new GetPresentationsByWalletQuery(walletId));
    }
}
