using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Queries.Tenants;

public class GetTenantStatisticsQuery : IQuery<TenantStatisticsDto>
{
    public Guid TenantId { get; }

    public GetTenantStatisticsQuery(Guid tenantId)
    {
        TenantId = tenantId;
    }
}