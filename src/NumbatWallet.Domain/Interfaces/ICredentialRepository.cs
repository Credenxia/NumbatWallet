using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Models;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Domain.Interfaces;

public interface ICredentialRepository : IRepository<Credential, Guid>
{
    // Specific queries
    Task<IReadOnlyList<Credential>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Credential>> GetByIssuerIdAsync(Guid issuerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Credential>> GetByTypeAsync(string credentialType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Credential>> GetByStatusAsync(CredentialStatus status, CancellationToken cancellationToken = default);
    Task<Credential?> GetByCredentialIdAsync(string credentialId, CancellationToken cancellationToken = default);

    // Complex queries
    Task<IReadOnlyList<Credential>> GetExpiringCredentialsAsync(
        int daysBeforeExpiry,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Credential>> GetRevokedCredentialsAsync(
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Credential>> GetBySchemaAsync(
        string schemaId,
        CancellationToken cancellationToken = default);

    // Pagination
    Task<PagedResponse<Credential>> GetPagedAsync(
        PagedRequest request,
        ISpecification<Credential>? specification = null,
        CancellationToken cancellationToken = default);

    // Statistics
    Task<int> CountByStatusAsync(CredentialStatus status, CancellationToken cancellationToken = default);
    Task<int> CountByTypeAsync(string credentialType, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetCredentialStatisticsAsync(CancellationToken cancellationToken = default);

    // Validation
    Task<bool> IsCredentialIdUniqueAsync(string credentialId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveCredentialOfTypeAsync(Guid walletId, string credentialType, CancellationToken cancellationToken = default);

    // Batch operations
    Task<IReadOnlyList<Credential>> GetBatchAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task RevokeExpiredCredentialsAsync(CancellationToken cancellationToken = default);
}