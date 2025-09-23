namespace NumbatWallet.Application.Queries.Tenants;

/// <summary>
/// Query to get tenant by ID
/// </summary>
public record GetTenantByIdQuery
{
    public Guid Id { get; init; }
}