using NumbatWallet.Domain.Entities;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Interfaces;

/// <summary>
/// Repository interface for wallet template management
/// </summary>
public interface IWalletTemplateRepository : IRepository<WalletTemplate, Guid>
{
    Task<IReadOnlyList<WalletTemplate>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WalletTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);
    Task<WalletTemplate?> GetByNameAsync(string name, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WalletTemplate>> GetByTypeAsync(WalletTemplateType type, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, Guid tenantId, CancellationToken cancellationToken = default);
}
