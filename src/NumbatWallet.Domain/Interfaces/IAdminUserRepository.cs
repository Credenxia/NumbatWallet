using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Domain.Interfaces;

/// <summary>
/// Repository interface for AdminUser entity
/// </summary>
public interface IAdminUserRepository
{
    /// <summary>
    /// Adds a new admin user
    /// </summary>
    Task<AdminUser> AddAsync(AdminUser adminUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing admin user
    /// </summary>
    Task<AdminUser> UpdateAsync(AdminUser adminUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an admin user
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets admin user by ID
    /// </summary>
    Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets admin user by email
    /// </summary>
    Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all admin users
    /// </summary>
    Task<IEnumerable<AdminUser>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets admin users by tenant
    /// </summary>
    Task<IEnumerable<AdminUser>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets admin users by role
    /// </summary>
    Task<IEnumerable<AdminUser>> GetByRoleAsync(string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an admin user exists by email
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
}