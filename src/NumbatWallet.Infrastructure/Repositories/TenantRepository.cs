using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Infrastructure.Data.Repositories;

namespace NumbatWallet.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Tenant operations
/// POA: Real database operations
/// </summary>
public class TenantRepository : RepositoryBase<Tenant, Guid>, ITenantRepository
{
    public TenantRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<Tenant?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(t => t.Identifier == identifier, cancellationToken);
    }

    public async Task<IEnumerable<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string identifier, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(t => t.Identifier == identifier, cancellationToken);
    }

    public async Task<(IEnumerable<Tenant> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = activeOnly
            ? DbSet.Where(t => t.IsActive)
            : DbSet.AsQueryable();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(t => t.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}