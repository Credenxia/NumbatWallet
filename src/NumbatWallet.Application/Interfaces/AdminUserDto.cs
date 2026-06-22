namespace NumbatWallet.Application.Interfaces;

// IUserManagementService (and its in-memory stub) was DELETED in the Credentry federation
// cleanup: user/role management is owned by the Credentry platform. Admin users are
// created/updated and have passwords reset in the Credentry portal
// (see credentry/docs/integration/06-NUMBATWALLET-FEDERATION-CONTRACT.md); NumbatWallet
// consumes identities via the CredentryJwt bearer scheme (NW.* role convention).
// AdminUserDto remains ONLY as the projection for the read-only adminUsers GraphQL query
// over the LEGACY admin_users table.

/// <summary>
/// Read-model projection of a row in the legacy <c>admin_users</c> table, served by the
/// read-only <c>adminUsers</c> GraphQL query. The table is no longer written to:
/// user lifecycle (create/update/roles/password) happens in the Credentry portal.
/// </summary>
public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName => $"{FirstName} {LastName}".Trim();
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
