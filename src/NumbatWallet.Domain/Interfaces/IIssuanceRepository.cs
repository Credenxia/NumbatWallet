using NumbatWallet.Domain.Entities;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Interfaces;

/// <summary>
/// Repository interface for issuance management
/// </summary>
public interface IIssuanceRepository : IRepository<Issuance, Guid>
{
    Task<IReadOnlyList<Issuance>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Issuance>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Issuance>> GetPendingIssuancesAsync(string? assignedTo = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Issuance>> GetByCredentialTypeAsync(string credentialType, CancellationToken cancellationToken = default);
    Task<Issuance?> GetByCredentialIdAsync(Guid credentialId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Issuance>> GetExpiringIssuancesAsync(DateTime expiryThreshold, CancellationToken cancellationToken = default);
}