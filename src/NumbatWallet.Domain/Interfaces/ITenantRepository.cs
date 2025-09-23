using NumbatWallet.Domain.Entities;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Interfaces;

/// <summary>
/// Repository interface for Tenant operations
/// </summary>
public interface ITenantRepository : IRepository<Tenant, Guid>
{
    /// <summary>
    /// Get tenant by identifier
    /// </summary>
    Task<Tenant?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active tenants
    /// </summary>
    Task<IEnumerable<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if tenant with identifier exists
    /// </summary>
    Task<bool> ExistsAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tenants with pagination
    /// </summary>
    Task<(IEnumerable<Tenant> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
}