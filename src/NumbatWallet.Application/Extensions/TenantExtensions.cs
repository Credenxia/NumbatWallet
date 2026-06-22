using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Application.Extensions;

/// <summary>
/// Extension methods for Tenant entity to DTO conversions
/// </summary>
public static class TenantExtensions
{
    /// <summary>
    /// Converts Tenant entity to TenantDto
    /// </summary>
    public static TenantDto ToDto(this Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        return new TenantDto
        {
            Id = tenant.Id.ToString(),
            Name = tenant.Name,
            Identifier = tenant.Identifier,
            IsActive = tenant.IsActive,
            ConnectionString = string.Empty, // Not exposed in entity
            Settings = new Dictionary<string, string>(),
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };
    }

    /// <summary>
    /// Converts collection of Tenant entities to TenantDto collection
    /// </summary>
    public static IEnumerable<TenantDto> ToDtos(this IEnumerable<Tenant> tenants)
    {
        ArgumentNullException.ThrowIfNull(tenants);
        return tenants.Select(t => t.ToDto());
    }
}