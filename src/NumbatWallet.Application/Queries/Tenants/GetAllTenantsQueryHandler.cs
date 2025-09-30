using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Extensions;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Queries.Tenants;

/// <summary>
/// Handler for getting all tenants
/// POA: Real implementation with extension methods for DTO conversion
/// </summary>
public class GetAllTenantsQueryHandler : IQueryHandler<GetAllTenantsQuery, IEnumerable<TenantDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<GetAllTenantsQueryHandler> _logger;

    public GetAllTenantsQueryHandler(
        ITenantRepository tenantRepository,
        ILogger<GetAllTenantsQueryHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<TenantDto>> HandleAsync(GetAllTenantsQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all tenants. ActiveOnly: {ActiveOnly}", query.ActiveOnly);

        var tenants = query.ActiveOnly
            ? await _tenantRepository.GetActiveTenantsAsync(cancellationToken)
            : await _tenantRepository.GetAllAsync(cancellationToken);

        var dtos = tenants.ToDtos();

        _logger.LogInformation("Retrieved {Count} tenants", dtos.Count());

        return dtos;
    }
}