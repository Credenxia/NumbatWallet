namespace NumbatWallet.Application.Queries.Tenants;

/// <summary>
/// Query to get all tenants
/// </summary>
public record GetAllTenantsQuery
{
    public bool ActiveOnly { get; init; } = true;
}