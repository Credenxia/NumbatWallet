using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Models;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Domain.Interfaces;

public interface IPersonRepository : IRepository<Person, Guid>
{
    // Specific queries
    Task<Person?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<Person?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Person?> GetByMobileNumberAsync(string mobileNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Person>> GetByStatusAsync(PersonStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Person>> GetVerifiedPersonsAsync(CancellationToken cancellationToken = default);

    // Complex queries
    Task<Person?> GetWithWalletsAsync(
        Guid personId,
        CancellationToken cancellationToken = default);

    Task<Person?> GetWithActiveWalletAsync(
        Guid personId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> GetByVerificationStatusAsync(
        bool isVerified,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> GetRecentlyCreatedAsync(
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    // Search
    Task<IReadOnlyList<Person>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default);

    // Pagination
    Task<PagedResponse<Person>> GetPagedAsync(
        PagedRequest request,
        ISpecification<Person>? specification = null,
        CancellationToken cancellationToken = default);

    // Statistics
    Task<int> CountByStatusAsync(PersonStatus status, CancellationToken cancellationToken = default);
    Task<int> CountVerifiedAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetPersonStatisticsAsync(CancellationToken cancellationToken = default);

    // Validation
    Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsMobileNumberUniqueAsync(string mobileNumber, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsExternalIdUniqueAsync(string externalId, CancellationToken cancellationToken = default);

    // Batch operations
    Task<IReadOnlyList<Person>> GetBatchAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Person>> GetByExternalIdsAsync(IEnumerable<string> externalIds, CancellationToken cancellationToken = default);
}