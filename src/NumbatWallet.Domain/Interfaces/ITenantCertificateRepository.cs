using NumbatWallet.Domain.Entities;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Interfaces;

public interface ITenantCertificateRepository : IRepository<TenantCertificate, Guid>
{
    Task<TenantCertificate?> GetByThumbprintAsync(string thumbprint, CancellationToken cancellationToken = default);
    Task<IEnumerable<TenantCertificate>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TenantCertificate>> GetActiveCertificatesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TenantCertificate>> GetExpiringCertificatesAsync(int daysBeforeExpiry, CancellationToken cancellationToken = default);
    Task<bool> ThumbprintExistsAsync(string thumbprint, CancellationToken cancellationToken = default);
    Task<IEnumerable<TenantCertificate>> GetByPurposeAsync(CertificatePurpose purpose, CancellationToken cancellationToken = default);
    Task<IEnumerable<TenantCertificate>> GetBySubjectDnAsync(string subjectDn, CancellationToken cancellationToken = default);
}
