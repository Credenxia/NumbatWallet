using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Infrastructure.Data.Repositories;

public class PresentationRepository : RepositoryBase<Presentation, Guid>, IPresentationRepository
{
    public PresentationRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Presentation>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.WalletId == walletId)
            .OrderByDescending(p => p.PresentedAt)
            .ToListAsync(cancellationToken);
    }
}
