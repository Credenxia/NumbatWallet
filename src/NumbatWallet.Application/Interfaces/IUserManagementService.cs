using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for managing system users and administrators
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Create a new user
    /// </summary>
    Task<SystemUserDto> CreateUserAsync(CreateSystemUserDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user information
    /// </summary>
    Task<SystemUserDto> UpdateUserAsync(Guid userId, UpdateSystemUserDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a user
    /// </summary>
    Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by ID
    /// </summary>
    Task<SystemUserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by email
    /// </summary>
    Task<SystemUserDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all users
    /// </summary>
    Task<List<SystemUserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Assign role to user
    /// </summary>
    Task<bool> AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove role from user
    /// </summary>
    Task<bool> RemoveRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset user password
    /// </summary>
    Task<bool> ResetPasswordAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset user password (string ID overload for GraphQL)
    /// </summary>
    Task<ResetPasswordResult> ResetPasswordAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create admin user (for GraphQL compatibility)
    /// </summary>
    Task<AdminUserDto> CreateAdminUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update admin user (for GraphQL compatibility)
    /// </summary>
    Task<AdminUserDto> UpdateAdminUserAsync(string id, UpdateUserCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lock user account
    /// </summary>
    Task<bool> LockUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlock user account
    /// </summary>
    Task<bool> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// System user data transfer object
/// </summary>
public class SystemUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// Create system user request
/// </summary>
public class CreateSystemUserDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Password { get; set; }
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Update system user request
/// </summary>
public class UpdateSystemUserDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool? IsActive { get; set; }
}

// Additional DTOs for GraphQL compatibility
public class CreateUserCommand
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class UpdateUserCommand
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public List<string>? Roles { get; set; }
    public bool? IsActive { get; set; }
}

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

public class ResetPasswordResult
{
    public bool Success { get; set; }
    public string TemporaryPassword { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}