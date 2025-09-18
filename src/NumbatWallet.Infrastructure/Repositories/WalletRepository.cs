using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Models;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Infrastructure.Repositories;

public class WalletRepository : RepositoryBase<Wallet, Guid>, IWalletRepository
{
    public WalletRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<Wallet?> GetByDidAsync(string did, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(w => w.WalletDid == did, cancellationToken);
    }

    public async Task<IReadOnlyList<Wallet>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(w => w.PersonId == personId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Wallet>> GetActiveWalletsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(w => w.Status == WalletStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Wallet>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(w => w.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DidExistsAsync(string did, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(w => w.WalletDid == did, cancellationToken);
    }

    public async Task<int> GetCredentialCountAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        // This would need to join with Credentials table
        var wallet = await Context.Wallets
            .Include("Credentials")
            .FirstOrDefaultAsync(w => w.Id == walletId, cancellationToken);

        return wallet?.GetCredentials()?.Count ?? 0;
    }

    public async Task<Dictionary<WalletStatus, int>> GetStatisticsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var statistics = await DbSet
            .Where(w => w.TenantId == tenantId)
            .GroupBy(w => w.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        // Ensure all statuses are represented
        foreach (var status in Enum.GetValues<WalletStatus>())
        {
            if (!statistics.ContainsKey(status))
            {
                statistics[status] = 0;
            }
        }

        return statistics;
    }

    public async Task<IReadOnlyList<Wallet>> GetByStatusAsync(WalletStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(w => w.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Wallet>> GetExpiringSoonAsync(int daysBeforeExpiry, CancellationToken cancellationToken = default)
    {
        var expiryDate = DateTimeOffset.UtcNow.AddDays(daysBeforeExpiry);
        return await DbSet
            .Where(w => w.ExpiresAt.HasValue && w.ExpiresAt.Value <= expiryDate && w.Status == WalletStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<Wallet?> GetActiveWalletByPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(w => w.PersonId == personId && w.Status == WalletStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Wallet>> GetWalletsWithCredentialsAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(w => w.Credentials)
            .Where(w => w.PersonId == personId && w.Credentials.Any())
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResponse<Wallet>> GetPagedAsync(
        PagedRequest request,
        ISpecification<Wallet>? specification = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (specification != null)
        {
            query = query.Where(specification.ToExpression());
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(w => w.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<Wallet>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    public async Task<int> CountActiveWalletsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(w => w.Status == WalletStatus.Active, cancellationToken);
    }

    public async Task<int> CountByStatusAsync(WalletStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(w => w.Status == status, cancellationToken);
    }

    public async Task<bool> IsDidUniqueAsync(string did, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(w => w.WalletDid == did);

        if (excludeId.HasValue)
        {
            query = query.Where(w => w.Id != excludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasActiveWalletAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(w => w.PersonId == personId && w.Status == WalletStatus.Active, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid personId, string walletName, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(w => w.PersonId == personId && w.Name == walletName, cancellationToken);
    }
}