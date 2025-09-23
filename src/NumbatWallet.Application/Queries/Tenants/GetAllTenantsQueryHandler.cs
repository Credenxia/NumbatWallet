using AutoMapper;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Queries.Tenants;

/// <summary>
/// Handler for getting all tenants
/// POA: Real implementation
/// </summary>
public class GetAllTenantsQueryHandler : IQueryHandler<GetAllTenantsQuery, IEnumerable<TenantDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllTenantsQueryHandler> _logger;

    public GetAllTenantsQueryHandler(
        ITenantRepository tenantRepository,
        IMapper mapper,
        ILogger<GetAllTenantsQueryHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<TenantDto>> HandleAsync(GetAllTenantsQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all tenants. ActiveOnly: {ActiveOnly}", query.ActiveOnly);

        var tenants = query.ActiveOnly
            ? await _tenantRepository.GetActiveTenantsAsync(cancellationToken)
            : await _tenantRepository.GetAllAsync(cancellationToken);

        var dtos = _mapper.Map<IEnumerable<TenantDto>>(tenants);

        _logger.LogInformation("Retrieved {Count} tenants", dtos.Count());

        return dtos;
    }
}