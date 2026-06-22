using System.Security.Claims;

namespace NumbatWallet.Web.Api.Authentication;

/// <summary>
/// Default implementation of IUserService for development/testing
/// In production, replace with actual database-backed implementation
/// </summary>
public class DefaultUserService : IUserService
{
    private readonly ILogger<DefaultUserService> _logger;

    public DefaultUserService(ILogger<DefaultUserService> logger)
    {
        _logger = logger;
    }

    public Task<UserInfo?> GetOrCreateUserAsync(string externalId, string provider, IEnumerable<Claim> claims)
    {
        _logger.LogInformation("Creating default user for {ExternalId} from {Provider}", externalId, provider);

        var claimsList = claims.ToList();
        var email = claimsList.FirstOrDefault(c => c.Type == "email" || c.Type == ClaimTypes.Email)?.Value ?? $"{externalId}@example.com";
        var name = claimsList.FirstOrDefault(c => c.Type == "name" || c.Type == ClaimTypes.Name)?.Value ?? "Default User";

        // Assign default roles based on provider
        var roles = new List<string>();
        if (provider == "AzureAd")
        {
            // Officers/admins from Azure AD
            roles.Add("Admin");
            roles.Add("Issuer");
        }
        else if (provider == "ServiceWA")
        {
            // Citizens from ServiceWA
            roles.Add("Holder");
        }

        var user = new UserInfo
        {
            Id = Guid.NewGuid().ToString(),
            ExternalId = externalId,
            Provider = provider,
            TenantId = "default-tenant",
            Email = email,
            Name = name,
            Roles = roles,
            Permissions = new List<string>
            {
                "wallet:view",
                "wallet:create",
                "credential:view"
            },
            CreatedAt = DateTimeOffset.UtcNow,
            LastLoginAt = DateTimeOffset.UtcNow
        };

        return Task.FromResult<UserInfo?>(user);
    }

    public Task UpdateLastLoginAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating last login for user {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task<UserInfo?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting user {UserId}", userId);
        return Task.FromResult<UserInfo?>(null);
    }
}
