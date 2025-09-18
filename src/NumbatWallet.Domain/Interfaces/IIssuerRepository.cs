using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Models;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Domain.Interfaces;

public interface IIssuerRepository : IRepository<Issuer, Guid>
{
    // Specific queries
    Task<Issuer?> GetByDidAsync(string did, CancellationToken cancellationToken = default);
    Task<Issuer?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Issuer>> GetByStatusAsync(IssuerStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Issuer>> GetActiveIssuersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Issuer>> GetTrustedIssuersAsync(CancellationToken cancellationToken = default);

    // Complex queries
    Task<Issuer?> GetWithCredentialsAsync(
        Guid issuerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Issuer>> GetByCredentialTypeAsync(
        string credentialType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Issuer>> GetByJurisdictionAsync(
        string jurisdiction,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Issuer>> GetExpiringCertificatesAsync(
        int daysBeforeExpiry,
        CancellationToken cancellationToken = default);

    // Search
    Task<IReadOnlyList<Issuer>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default);

    // Pagination
    Task<PagedResponse<Issuer>> GetPagedAsync(
        PagedRequest request,
        ISpecification<Issuer>? specification = null,
        CancellationToken cancellationToken = default);

    // Statistics
    Task<int> CountByStatusAsync(IssuerStatus status, CancellationToken cancellationToken = default);
    Task<int> CountTrustedAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetIssuerStatisticsAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetCredentialTypeStatisticsAsync(CancellationToken cancellationToken = default);

    // Validation
    Task<bool> IsDidUniqueAsync(string did, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsExternalIdUniqueAsync(string externalId, CancellationToken cancellationToken = default);
    Task<bool> CanIssueCredentialTypeAsync(Guid issuerId, string credentialType, CancellationToken cancellationToken = default);

    // Batch operations
    Task<IReadOnlyList<Issuer>> GetBatchAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Issuer>> GetByDidsAsync(IEnumerable<string> dids, CancellationToken cancellationToken = default);

    // Trust registry operations
    Task<bool> IsTrustedIssuerAsync(string did, CancellationToken cancellationToken = default);
    Task UpdateTrustStatusAsync(Guid issuerId, bool isTrusted, CancellationToken cancellationToken = default);
}