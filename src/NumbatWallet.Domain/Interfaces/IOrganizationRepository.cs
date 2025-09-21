using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Interfaces;

public interface IOrganizationRepository : IRepository<Organization, Guid>
{
    Task<Organization?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Organization>> GetByTypeAsync(string type, CancellationToken cancellationToken = default);
}