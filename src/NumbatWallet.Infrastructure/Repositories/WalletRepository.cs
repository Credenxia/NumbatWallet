using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Infrastructure.Repositories;

public class WalletRepository : RepositoryBase<Wallet, Guid>, IWalletRepository
{
    public WalletRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<Wallet?> GetByDidAsync(string did, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(w => w.WalletDid == did, cancellationToken);
    }

    public async Task<IReadOnlyList<Wallet>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(w => w.PersonId == personId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Wallet>> GetActiveWalletsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(w => w.Status == WalletStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Wallet>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(w => w.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DidExistsAsync(string did, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(w => w.WalletDid == did, cancellationToken);
    }

    public async Task<int> GetCredentialCountAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        // This would need to join with Credentials table
        var wallet = await _context.Wallets
            .Include("Credentials")
            .FirstOrDefaultAsync(w => w.Id == walletId, cancellationToken);

        return wallet?.GetCredentials()?.Count ?? 0;
    }

    public async Task<Dictionary<WalletStatus, int>> GetStatisticsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var statistics = await _dbSet
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
}