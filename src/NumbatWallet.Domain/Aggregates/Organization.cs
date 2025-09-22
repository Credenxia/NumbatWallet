using NumbatWallet.Domain.Enums;
using NumbatWallet.SharedKernel.Primitives;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Aggregates;

public class Organization : AuditableEntity<Guid>, ITenantAware
{
    private Organization() : base(Guid.NewGuid()) { } // EF Core constructor

    public string Name { get; private set; } = string.Empty;
    public OrganizationType Type { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public string TenantId { get; private set; } = string.Empty;

    public void SetTenantId(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));
        TenantId = tenantId;
    }

    public static Organization Create(string name, string type, string? description = null, string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(type, nameof(type));

        var organization = new Organization
        {
            Name = name,
            Type = Enum.Parse<OrganizationType>(type, true),
            Description = description,
            IsActive = true,
            TenantId = tenantId ?? "default" // Default tenant for POA
        };

        return organization;
    }

    public void UpdateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name;
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
