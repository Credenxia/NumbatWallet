using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;

namespace NumbatWallet.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for AdminUser entity
/// </summary>
public class AdminUserRepository : IAdminUserRepository
{
    private readonly NumbatWalletDbContext _context;

    public AdminUserRepository(NumbatWalletDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<AdminUser> AddAsync(AdminUser adminUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(adminUser);

        await _context.AdminUsers.AddAsync(adminUser, cancellationToken);
        // Note: SaveChanges is handled by UnitOfWork pattern
        return adminUser;
    }

    public async Task<AdminUser> UpdateAsync(AdminUser adminUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(adminUser);

        _context.AdminUsers.Update(adminUser);
        // Note: SaveChanges is handled by UnitOfWork pattern
        return adminUser;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var adminUser = await GetByIdAsync(id, cancellationToken);
        if (adminUser == null)
        {
            return false;
        }

        _context.AdminUsers.Remove(adminUser);
        // Note: SaveChanges is handled by UnitOfWork pattern
        return true;
    }

    public async Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<AdminUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AdminUsers
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AdminUser>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.AdminUsers
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AdminUser>> GetByRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        // EF Core doesn't support querying collections in where clauses directly for value types
        // We need to fetch all and filter in memory, or use raw SQL
        var allUsers = await _context.AdminUsers.ToListAsync(cancellationToken);
        return allUsers.Where(u => u.Roles.Contains(roleName));
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.AdminUsers
            .AnyAsync(u => u.Email == email, cancellationToken);
    }
}