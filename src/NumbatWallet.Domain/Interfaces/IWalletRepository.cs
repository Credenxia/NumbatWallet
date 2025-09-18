using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Models;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Domain.Interfaces;

public interface IWalletRepository : IRepository<Wallet, Guid>
{
    // Specific queries
    Task<Wallet?> GetByDidAsync(string did, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Wallet>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Wallet>> GetByStatusAsync(WalletStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Wallet>> GetActiveWalletsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Wallet>> GetExpiringSoonAsync(int daysBeforeExpiry, CancellationToken cancellationToken = default);

    // Complex queries
    Task<Wallet?> GetActiveWalletByPersonAsync(
        Guid personId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Wallet>> GetWalletsWithCredentialsAsync(
        Guid personId,
        CancellationToken cancellationToken = default);

    // Pagination
    Task<PagedResponse<Wallet>> GetPagedAsync(
        PagedRequest request,
        ISpecification<Wallet>? specification = null,
        CancellationToken cancellationToken = default);

    // Statistics
    Task<int> CountActiveWalletsAsync(CancellationToken cancellationToken = default);
    Task<int> CountByStatusAsync(WalletStatus status, CancellationToken cancellationToken = default);

    // Validation
    Task<bool> IsDidUniqueAsync(string did, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasActiveWalletAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid personId, string walletName, CancellationToken cancellationToken = default);
}