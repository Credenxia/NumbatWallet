using AutoMapper;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Queries.Tenants;

/// <summary>
/// Handler for getting tenant by ID
/// POA: Real implementation
/// </summary>
public class GetTenantByIdQueryHandler : IQueryHandler<GetTenantByIdQuery, TenantDto?>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetTenantByIdQueryHandler> _logger;

    public GetTenantByIdQueryHandler(
        ITenantRepository tenantRepository,
        IMapper mapper,
        ILogger<GetTenantByIdQueryHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<TenantDto?> HandleAsync(GetTenantByIdQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting tenant with ID: {TenantId}", query.Id);

        var tenant = await _tenantRepository.GetByIdAsync(query.Id, cancellationToken);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant with ID {TenantId} not found", query.Id);
            return null;
        }

        return _mapper.Map<TenantDto>(tenant);
    }
}