using NumbatWallet.Domain.Entities;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Interfaces;

public interface ICertificateAuthorityRepository : IRepository<CertificateAuthority, Guid>
{
    Task<CertificateAuthority?> GetByThumbprintAsync(string thumbprint, CancellationToken cancellationToken = default);
    Task<CertificateAuthority?> GetBySubjectDnAsync(string subjectDn, CancellationToken cancellationToken = default);
    Task<IEnumerable<CertificateAuthority>> GetTrustedAuthoritiesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CertificateAuthority>> GetByTrustLevelAsync(CertificateTrustLevel minTrustLevel, CancellationToken cancellationToken = default);
    Task<bool> IsAuthorityTrustedAsync(string thumbprint, CancellationToken cancellationToken = default);
}