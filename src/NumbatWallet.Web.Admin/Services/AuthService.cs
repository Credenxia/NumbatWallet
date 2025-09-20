using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Blazored.SessionStorage;

namespace NumbatWallet.Web.Admin.Services;

public class AuthService : IAuthService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IApiClient _apiClient;
    private readonly ISessionStorageService _sessionStorage;
    private readonly ILogger<AuthService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private const string UserInfoKey = "userInfo";
    private const string PermissionsKey = "permissions";
    private const string TokenKey = "access_token";

    public AuthService(
        AuthenticationStateProvider authenticationStateProvider,
        IApiClient apiClient,
        ISessionStorageService sessionStorage,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _apiClient = apiClient;
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
        // Try to get from session storage first
        try
        {
            var cachedUser = await _sessionStorage.GetItemAsync<UserInfo>(UserInfoKey);
            if (cachedUser != null)
            {
                return cachedUser;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get user from session storage");
        }

        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return null;
        }

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

        // Cache in session storage
        try
        {
            await _sessionStorage.SetItemAsync(UserInfoKey, userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache user in session storage");
        }

        return userInfo;
    }

    public async Task<List<string>> GetUserPermissionsAsync(string userId)
    {
        // Try to get from session storage first
        try
        {
            var cacheKey = $"{PermissionsKey}:{userId}";
            var cachedPermissions = await _sessionStorage.GetItemAsync<List<string>>(cacheKey);

            if (cachedPermissions != null)
            {
                return cachedPermissions;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get permissions from session storage");
        }

        try
        {
            // Fetch from API
            var permissions = await _apiClient.GetAsync<List<string>>(
                $"/api/admin/users/{userId}/permissions");

            // Cache for 5 minutes
            if (permissions != null)
            {
                try
                {
                    await _sessionStorage.SetItemAsync(
                        $"{PermissionsKey}:{userId}",
                        permissions);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache permissions in session storage");
                }
            }

            return permissions ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch permissions for user {UserId}", userId);
            return new List<string>();
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
            // Try to get from session storage
            var token = await _sessionStorage.GetItemAsStringAsync(TokenKey);
            if (!string.IsNullOrEmpty(token))
            {
                return token;
            }

            // Try to get from HTTP context
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                token = await httpContext.GetTokenAsync("access_token");
                if (!string.IsNullOrEmpty(token))
                {
                    // Cache the token
                    await _sessionStorage.SetItemAsStringAsync(TokenKey, token);
                    return token;
                }
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