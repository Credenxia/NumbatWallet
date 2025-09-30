using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Infrastructure.Data.Repositories;

public class OrganizationRepository : RepositoryBase<Organization, Guid>, IOrganizationRepository
{
    public OrganizationRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<Organization?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(o => o.Name == name, cancellationToken);
    }

    public async Task<bool> IsNameUniqueAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(o => o.Name == name);
        if (excludeId.HasValue)
        {
            query = query.Where(o => o.Id != excludeId.Value);
        }
        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<IEnumerable<Organization>> GetByTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<Domain.Enums.OrganizationType>(type, true, out var orgType))
        {
            return Enumerable.Empty<Organization>();
        }
        return await DbSet.Where(o => o.Type == orgType).ToListAsync(cancellationToken);
    }
}
