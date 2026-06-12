namespace NumbatWallet.Domain.Entities;

/// <summary>
/// LEGACY admin user entity (table <c>admin_users</c>). User/role management is owned by
/// the Credentry platform since the federation cleanup
/// (credentry/docs/integration/06-NUMBATWALLET-FEDERATION-CONTRACT.md): admin identities
/// are created/updated in the Credentry portal and arrive as CredentryJwt bearer tokens
/// with NW.* roles. This table is no longer written to by the API; it remains readable
/// via the read-only <c>adminUsers</c> GraphQL query.
/// </summary>
public class AdminUser
{
    private readonly List<string> _roles = new();

    private AdminUser() { } // For EF Core

    public AdminUser(
        string email,
        string firstName,
        string lastName,
        Guid tenantId)
    {
        Id = Guid.NewGuid();
        Email = email ?? throw new ArgumentNullException(nameof(email));
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        TenantId = tenantId;
        IsActive = true;
        IsLocked = false;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string DisplayName => $"{FirstName} {LastName}".Trim();
    public IReadOnlyCollection<string> Roles => _roles.AsReadOnly();
    public bool IsActive { get; private set; }
    public bool IsLocked { get; private set; }
    public string? LockReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? LastPasswordChangeAt { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Updates user information
    /// </summary>
    public void UpdateInfo(string? firstName, string? lastName, string? email)
    {
        if (!string.IsNullOrWhiteSpace(firstName))
        {
            FirstName = firstName;
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            LastName = lastName;
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            Email = email;
        }
    }

    /// <summary>
    /// Assigns a role to the user
    /// </summary>
    public void AssignRole(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("Role name cannot be empty", nameof(roleName));
        }

        if (!_roles.Contains(roleName))
        {
            _roles.Add(roleName);
        }
    }

    /// <summary>
    /// Removes a role from the user
    /// </summary>
    public void RemoveRole(string roleName)
    {
        _roles.Remove(roleName);
    }

    /// <summary>
    /// Activates the user account
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivates the user account
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Locks the user account
    /// </summary>
    public void Lock(string reason)
    {
        IsLocked = true;
        LockReason = reason;
    }

    /// <summary>
    /// Unlocks the user account
    /// </summary>
    public void Unlock()
    {
        IsLocked = false;
        LockReason = null;
    }

    /// <summary>
    /// Records a successful login
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a password change
    /// </summary>
    public void RecordPasswordChange()
    {
        LastPasswordChangeAt = DateTime.UtcNow;
    }
}