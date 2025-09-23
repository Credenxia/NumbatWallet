using Microsoft.EntityFrameworkCore;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Primitives;

namespace NumbatWallet.Infrastructure.Data.Repositories;

/// <summary>
/// Generic repository implementation for entities
/// POA: Provides concrete implementation for DI container
/// </summary>
public class GenericRepository<TEntity, TId> : RepositoryBase<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    public GenericRepository(NumbatWalletDbContext context) : base(context)
    {
    }
}