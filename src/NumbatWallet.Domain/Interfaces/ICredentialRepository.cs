using System.Linq.Expressions;
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

    // Additional methods for search and pagination
    Task<int> CountAsync(ISpecification<Credential> specification, CancellationToken cancellationToken = default);
    Task<IEnumerable<Credential>> GetPagedAsync(
        ISpecification<Credential> specification,
        int skip,
        int take,
        Expression<Func<Credential, object>>? orderBy = null,
        bool descending = false,
        CancellationToken cancellationToken = default);
}
