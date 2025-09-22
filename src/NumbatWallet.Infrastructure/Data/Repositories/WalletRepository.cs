using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Infrastructure.Data.Repositories;

public class WalletRepository : RepositoryBase<Wallet, Guid>, IWalletRepository
{
    public WalletRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<Wallet?> GetByDidAsync(string walletDid, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(w => w.WalletDid == walletDid, cancellationToken);
    }

    public async Task<IEnumerable<Wallet>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(w => w.PersonId == personId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Wallet>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // TODO: Add TenantId to Wallet entity
        return await DbSet.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Wallet>> GetActiveWalletsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(w => w.Status == SharedKernel.Enums.WalletStatus.Active).ToListAsync(cancellationToken);
    }

    public async Task<bool> WalletExistsForPersonAsync(Guid personId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(w => w.PersonId == personId, cancellationToken);
    }

    public async Task<Wallet?> GetWithCredentialsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include("Credentials")
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<bool> IsDidUniqueAsync(string did, CancellationToken cancellationToken = default)
    {
        return !await DbSet.AnyAsync(w => w.WalletDid == did, cancellationToken);
    }
}