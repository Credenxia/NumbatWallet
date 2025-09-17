using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Repositories;

public interface IWalletRepository : IRepository<Wallet, Guid>
{
    Task<IEnumerable<Wallet>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<Wallet?> GetByDidAsync(string walletDid, CancellationToken cancellationToken = default);
    Task<IEnumerable<Wallet>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Wallet>> GetActiveWalletsAsync(CancellationToken cancellationToken = default);
    Task<bool> WalletExistsForPersonAsync(Guid personId, Guid tenantId, CancellationToken cancellationToken = default);
}