using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Domain.Interfaces;

public interface ICredentialRepository : IRepository<Credential, Guid>
{
    Task<IEnumerable<Credential>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Credential>> GetByIssuerIdAsync(Guid issuerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Credential>> GetActiveCredentialsAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Credential>> GetExpiredCredentialsAsync(CancellationToken cancellationToken = default);
    Task<Credential?> GetByWalletAndTypeAsync(Guid walletId, string credentialType, CancellationToken cancellationToken = default);
    Task<IEnumerable<Credential>> FindAsync(ISpecification<Credential> specification, CancellationToken cancellationToken = default);
}