using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.SharedKernel.Attributes;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;
using System.Reflection;

namespace NumbatWallet.Infrastructure.Data.Interceptors;

/// <summary>
/// Interceptor that logs access to protected fields based on data classification
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditInterceptor> _logger;

    public AuditInterceptor(IServiceProvider serviceProvider, ILogger<AuditInterceptor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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

        using var scope = _serviceProvider.CreateScope();
        var auditService = scope.ServiceProvider.GetService<IAuditService>();
        var currentUserService = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
        var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();

        if (auditService == null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var auditEntries = new List<AuditEntry>();

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Unchanged || entry.State == EntityState.Detached)
            {
                continue;
            }

            var auditEntry = CreateAuditEntry(entry, currentUserService.UserId, tenantService.TenantId);
            if (auditEntry != null)
            {
                auditEntries.Add(auditEntry);
            }
        }

        // Log audit entries
        foreach (var auditEntry in auditEntries)
        {
            await auditService.LogAccessAsync(auditEntry, cancellationToken);

            // Log sensitive data access
            if (auditEntry.HasSensitiveData)
            {
                _logger.LogInformation(
                    "Sensitive data accessed: Entity={EntityType}, Action={Action}, User={UserId}, Classification={Classification}",
                    auditEntry.EntityType,
                    auditEntry.Action,
                    auditEntry.UserId,
                    auditEntry.MaxClassification);
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private AuditEntry? CreateAuditEntry(EntityEntry entry, string userId, Guid tenantId)
    {
        var entityType = entry.Entity.GetType();

        var auditEntry = new AuditEntry
        {
            EntityType = entityType.Name,
            EntityId = GetEntityId(entry),
            Action = entry.State.ToString(),
            UserId = userId,
            TenantId = tenantId,
            Timestamp = DateTimeOffset.UtcNow,
            ChangedProperties = new Dictionary<string, PropertyAuditInfo>()
        };

        var maxClassification = DataClassification.Unofficial;
        var hasSensitiveData = false;

        foreach (var property in entry.Properties)
        {
            if (!property.IsModified && entry.State != EntityState.Added)
            {
                continue;
            }

            var propertyInfo = entityType.GetProperty(property.Metadata.Name);
            if (propertyInfo == null)
            {
                continue;
            }

            var classificationAttr = propertyInfo.GetCustomAttribute<DataClassificationAttribute>();
            var classification = classificationAttr?.Classification ?? DataClassification.Unofficial;

            if (classification > maxClassification)
            {
                maxClassification = classification;
            }

            if (classification >= DataClassification.OfficialSensitive)
            {
                hasSensitiveData = true;
            }

            var auditInfo = new PropertyAuditInfo
            {
                PropertyName = propertyInfo.Name,
                Classification = classification,
                Purpose = classificationAttr?.Purpose,
                OldValue = entry.State == EntityState.Modified ? GetRedactedValue(property.OriginalValue, classification) : null,
                NewValue = GetRedactedValue(property.CurrentValue, classification),
                IsProtected = classification >= DataClassification.OfficialSensitive
            };

            auditEntry.ChangedProperties[propertyInfo.Name] = auditInfo;
        }

        auditEntry.MaxClassification = maxClassification;
        auditEntry.HasSensitiveData = hasSensitiveData;

        return auditEntry;
    }

    private string GetEntityId(EntityEntry entry)
    {
        var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties;
        if (keyProperties == null || !keyProperties.Any())
        {
            return "Unknown";
        }

        var keyValues = keyProperties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "null");

        return string.Join(",", keyValues);
    }

    private string GetRedactedValue(object? value, DataClassification classification)
    {
        if (value == null)
        {
            return "null";
        }

        // For highly sensitive data, don't log the actual value
        if (classification >= DataClassification.Protected)
        {
            return "***REDACTED***";
        }

        // For sensitive data, log a partial value
        if (classification >= DataClassification.OfficialSensitive)
        {
            var stringValue = value.ToString() ?? "";
            if (stringValue.Length <= 2)
            {
                return "**";
            }

            return $"{stringValue[0]}***{stringValue[^1]}";
        }

        // For non-sensitive data, log the full value (truncated if too long)
        var fullValue = value.ToString() ?? "";
        return fullValue.Length > 100 ? string.Concat(fullValue.AsSpan(0, 97), "...") : fullValue;
    }
}

public class AuditEntry
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public DataClassification MaxClassification { get; set; }
    public bool HasSensitiveData { get; set; }
    public Dictionary<string, PropertyAuditInfo> ChangedProperties { get; set; } = new();
}

public class PropertyAuditInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public DataClassification Classification { get; set; }
    public string? Purpose { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public bool IsProtected { get; set; }
}