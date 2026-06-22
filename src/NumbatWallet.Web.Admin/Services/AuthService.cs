using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Blazored.SessionStorage;

namespace NumbatWallet.Web.Admin.Services;

public class AuthService : IAuthService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ISessionStorageService _sessionStorage;
    private readonly ILogger<AuthService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private const string UserInfoKey = "userInfo";
    private const string PermissionsKey = "permissions";
    private const string TokenKey = "access_token";

    public AuthService(
        AuthenticationStateProvider authenticationStateProvider,
        ISessionStorageService sessionStorage,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _sessionStorage = sessionStorage;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> EnhanceClaimsPrincipalAsync(ClaimsPrincipal principal)
    {
        if (!principal.Identity?.IsAuthenticated ?? true)
        {
            return principal;
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return principal;
        }

        try
        {
            // Get additional permissions from API
            var permissions = await GetUserPermissionsAsync(userId);

            var identity = principal.Identity as ClaimsIdentity;
            foreach (var permission in permissions)
            {
                identity?.AddClaim(new Claim("permission", permission));
            }

            // Add tenant information if available
            var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"]?.ToString();
            if (!string.IsNullOrEmpty(tenantId))
            {
                identity?.AddClaim(new Claim("tenant", tenantId));
            }

            return identity != null ? new ClaimsPrincipal(identity) : principal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enhance claims for user {UserId}", userId);
            return principal;
        }
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return null;
        }

        // Build user info from claims (works during prerendering)
        var userInfo = new UserInfo
        {
            Id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            Email = user.FindFirst(ClaimTypes.Email)?.Value,
            Name = user.FindFirst(ClaimTypes.Name)?.Value,
            FirstName = user.FindFirst(ClaimTypes.GivenName)?.Value,
            LastName = user.FindFirst(ClaimTypes.Surname)?.Value,
            Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
            Permissions = user.FindAll("permission").Select(c => c.Value).ToList(),
            TenantId = user.FindFirst("tenant")?.Value,
            LastLogin = DateTime.UtcNow
        };

        // Try to cache in session storage (skip during prerendering when JS not available)
        try
        {
            await _sessionStorage.SetItemAsync(UserInfoKey, userInfo);
        }
        catch
        {
            // Ignore - session storage not available during prerendering
        }

        return userInfo;
    }

    public async Task<List<string>> GetUserPermissionsAsync(string userId)
    {
        // REMOVED: API call to avoid circular dependency with ApiClient
        // Permissions should come from claims in the authentication token
        // If additional permissions are needed, components should call API directly

        try
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user?.Identity?.IsAuthenticated == true)
            {
                // Get permissions from claims
                var permissions = user.FindAll("permission")
                    .Select(c => c.Value)
                    .ToList();

                return permissions;
            }

            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permissions for user {UserId}", userId);
            return [];
        }
    }

    public async Task<bool> HasPermissionAsync(string permission)
    {
        var user = await GetCurrentUserAsync();
        return user?.Permissions?.Contains(permission) ?? false;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            // Try to get from HTTP context first (works during prerendering)
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var token = await httpContext.GetTokenAsync("access_token");
                if (!string.IsNullOrEmpty(token))
                {
                    // Try to cache the token in session storage (skip if JS not available)
                    try
                    {
                        await _sessionStorage.SetItemAsStringAsync(TokenKey, token);
                    }
                    catch
                    {
                        // Ignore - JS interop not available during prerendering
                    }
                    return token;
                }
            }

            // Try session storage only as fallback (may not work during prerendering)
            try
            {
                var cachedToken = await _sessionStorage.GetItemAsStringAsync(TokenKey);
                if (!string.IsNullOrEmpty(cachedToken))
                {
                    return cachedToken;
                }
            }
            catch
            {
                // Ignore - JS interop not available during prerendering
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get access token");
        }

        return null;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User.Identity?.IsAuthenticated ?? false;
    }

    public async Task SignOutAsync()
    {
        try
        {
            // Clear session storage
            await _sessionStorage.ClearAsync();

            // Navigate to logout endpoint
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                httpContext.Response.Redirect("/logout");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sign out");
        }
    }
}
