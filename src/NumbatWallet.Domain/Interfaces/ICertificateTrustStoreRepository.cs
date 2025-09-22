using NumbatWallet.Domain.Entities;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Interfaces;

public interface ICertificateTrustStoreRepository : IRepository<CertificateTrustStore, Guid>
{
    Task<CertificateTrustStore?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<CertificateTrustStore?> GetActiveByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CertificateTrustStore>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> IsCertificateRevokedAsync(string thumbprint, Guid tenantId, CancellationToken cancellationToken = default);
}
