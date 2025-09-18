using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NumbatWallet.SharedKernel.Interfaces;
using System.Linq.Expressions;

namespace NumbatWallet.Infrastructure.Data.Interceptors;

public class TenantInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentTenantService _currentTenantService;

    public TenantInterceptor(ICurrentTenantService currentTenantService)
    {
        _currentTenantService = currentTenantService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyTenantFilter(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTenantFilter(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyTenantFilter(DbContext? context)
    {
        if (context == null) return;

        var tenantId = _currentTenantService.TenantId;
        if (string.IsNullOrEmpty(tenantId)) return;

        // Set TenantId for new entities
        var addedEntries = context.ChangeTracker
            .Entries<ITenantAware>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in addedEntries)
        {
            if (string.IsNullOrEmpty(entry.Entity.TenantId))
            {
                entry.Entity.TenantId = tenantId;
            }
            else if (entry.Entity.TenantId != tenantId)
            {
                throw new InvalidOperationException(
                    $"Entity TenantId '{entry.Entity.TenantId}' does not match current tenant '{tenantId}'");
            }
        }

        // Validate TenantId for modified entities
        var modifiedEntries = context.ChangeTracker
            .Entries<ITenantAware>()
            .Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted);

        foreach (var entry in modifiedEntries)
        {
            if (entry.Entity.TenantId != tenantId)
            {
                throw new InvalidOperationException(
                    $"Cannot modify entity from different tenant. Entity TenantId: '{entry.Entity.TenantId}', Current TenantId: '{tenantId}'");
            }
        }
    }
}

public static class TenantQueryExtensions
{
    public static void ApplyTenantQueryFilter(this ModelBuilder modelBuilder, ICurrentTenantService currentTenantService)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantAware).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(TenantQueryExtensions)
                    .GetMethod(nameof(GetTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.MakeGenericMethod(entityType.ClrType);

                if (method != null)
                {
                    var filter = method.Invoke(null, new object[] { currentTenantService });
                    if (filter != null)
                    {
                        entityType.SetQueryFilter((LambdaExpression)filter);
                    }
                }
            }
        }
    }

    private static LambdaExpression GetTenantFilter<TEntity>(ICurrentTenantService currentTenantService)
        where TEntity : class, ITenantAware
    {
        Expression<Func<TEntity, bool>> filter = e => e.TenantId == currentTenantService.TenantId;
        return filter;
    }
}