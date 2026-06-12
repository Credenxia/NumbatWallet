using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Interfaces;

public interface IPresentationRepository : IRepository<Presentation, Guid>
{
    Task<IEnumerable<Presentation>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default);
}
