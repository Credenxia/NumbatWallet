using System.Security.Claims;

namespace NumbatWallet.Web.Api.Authentication;

/// <summary>
/// Service for user provisioning and claim enrichment during authentication
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets an existing user or creates a new one based on OIDC claims
    /// </summary>
    /// <param name="externalId">External user ID from identity provider (sub claim)</param>
    /// <param name="provider">Identity provider name (AzureAd, ServiceWA)</param>
    /// <param name="claims">Claims from the identity provider</param>
    /// <returns>User with roles and permissions, or null if user creation fails</returns>
    Task<UserInfo?> GetOrCreateUserAsync(string externalId, string provider, IEnumerable<Claim> claims);

    /// <summary>
    /// Updates user's last login timestamp
    /// </summary>
    Task UpdateLastLoginAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user by internal ID
    /// </summary>
    Task<UserInfo?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// User information with roles and permissions for claim enrichment
/// </summary>
public class UserInfo
{
    public required string Id { get; init; }
    public required string ExternalId { get; init; }
    public required string Provider { get; init; }
    public required string TenantId { get; init; }
    public required string Email { get; init; }
    public string? Name { get; init; }
    public List<string> Roles { get; init; } = new();
    public List<string> Permissions { get; init; } = new();
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
}
