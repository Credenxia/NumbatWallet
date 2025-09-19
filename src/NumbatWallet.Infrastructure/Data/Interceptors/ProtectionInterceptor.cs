using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.SharedKernel.Attributes;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;
using System.Reflection;

namespace NumbatWallet.Infrastructure.Data.Interceptors;

/// <summary>
/// Interceptor that applies protection (encryption/tokenization) based on tenant policies
/// </summary>
public class ProtectionInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public ProtectionInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        // Get services from DI
        using var scope = _serviceProvider.CreateScope();
        var protectionService = scope.ServiceProvider.GetService<IProtectionService>();
        var searchTokenService = scope.ServiceProvider.GetService<ISearchTokenService>();
        var tenantPolicyService = scope.ServiceProvider.GetService<ITenantPolicyService>();
        var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();

        // Only apply protection if services are available
        if (protectionService == null || searchTokenService == null || tenantPolicyService == null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var tenantId = tenantService.TenantId;

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                await ApplyProtectionToEntity(
                    entry,
                    protectionService,
                    searchTokenService,
                    tenantPolicyService,
                    tenantId,
                    cancellationToken);
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task ApplyProtectionToEntity(
        EntityEntry entry,
        IProtectionService protectionService,
        ISearchTokenService searchTokenService,
        ITenantPolicyService tenantPolicyService,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var entityType = entry.Entity.GetType();

        // Process each property
        foreach (var property in entry.Properties)
        {
            var propertyInfo = entityType.GetProperty(property.Metadata.Name);
            if (propertyInfo == null)
            {
                continue;
            }

            // Check if property has DataClassification attribute
            var classificationAttr = propertyInfo.GetCustomAttribute<DataClassificationAttribute>();
            if (classificationAttr == null)
            {
                continue;
            }

            // Get the field policy from tenant configuration
            var fieldPolicy = await tenantPolicyService.GetFieldPolicyAsync(
                tenantId,
                entityType.Name,
                propertyInfo.Name);

            // Skip if no protection required for this field
            if (!fieldPolicy.EnableEncryption && !fieldPolicy.EnableTokenization)
            {
                continue;
            }

            // Get the current value
            var currentValue = property.CurrentValue;
            if (currentValue == null)
            {
                continue;
            }

            // Apply protection based on policy
            if (currentValue is string stringValue)
            {
                var protectedValue = await protectionService.ProtectAsync(
                    stringValue,
                    classificationAttr.Classification,
                    propertyInfo.Name,
                    entityType.Name);

                // Generate search tokens if needed
                if (fieldPolicy.SearchStrategy != SearchStrategy.None)
                {
                    var tokens = await searchTokenService.GenerateTokensAsync(
                        stringValue,
                        fieldPolicy.SearchStrategy,
                        entityType.Name,
                        propertyInfo.Name);

                    // Store tokens in a shadow property or separate table
                    // This would be implemented based on your search requirements
                    await StoreSearchTokens(entry, propertyInfo.Name, tokens);
                }

                // The actual value conversion to JSONB is handled by ProtectedFieldConverter
                // We just ensure the value is properly marked for protection
                property.CurrentValue = stringValue; // Keep original, converter will handle it
            }
        }
    }

    private async Task StoreSearchTokens(
        EntityEntry entry,
        string propertyName,
        IEnumerable<string> tokens)
    {
        // Implementation would store search tokens for the field
        // This could be in a shadow property, separate table, or within the JSONB structure
        // For now, this is a placeholder
        await Task.CompletedTask;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        // Synchronous version - just call async version
        var task = SavingChangesAsync(eventData, result);
        if (task.IsCompleted)
        {
            return task.Result;
        }
        return task.AsTask().GetAwaiter().GetResult();
    }
}