namespace NumbatWallet.Application.DTOs;

public class TenantDto
{
    public string Id { get; set; } = string.Empty;
    public string TenantId => Id; // Alias for compatibility
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    // SECURITY: the per-tenant DB connection string must NEVER be reachable through the
    // API/GraphQL. The admin `tenants`/`tenant` queries project TenantDto directly, so this
    // field is excluded from the GraphQL schema in Web.Api via TenantDtoType (an
    // IObjectTypeDescriptor that Ignore()s it). The Application layer stays free of any
    // GraphQL dependency. The model property is retained because TenantService populates it
    // for internal connection routing.
    public string ConnectionString { get; set; } = string.Empty;
    public Dictionary<string, string> Settings { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; }
    public string? TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
