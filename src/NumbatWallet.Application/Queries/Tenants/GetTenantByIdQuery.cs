using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Queries.Tenants;

/// <summary>
/// Query to get tenant by ID
/// </summary>
public record GetTenantByIdQuery : IQuery<TenantDto?>
{
    public Guid Id { get; init; }
}