using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Queries.Tenants;

/// <summary>
/// Query to get all tenants
/// </summary>
public record GetAllTenantsQuery : IQuery<IEnumerable<TenantDto>>
{
    public bool ActiveOnly { get; init; } = true;
}