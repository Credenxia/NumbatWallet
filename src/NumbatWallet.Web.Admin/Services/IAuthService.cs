using System.Security.Claims;

namespace NumbatWallet.Web.Admin.Services;

public interface IAuthService
{
    Task<ClaimsPrincipal> EnhanceClaimsPrincipalAsync(ClaimsPrincipal principal);
    Task<UserInfo?> GetCurrentUserAsync();
    Task<List<string>> GetUserPermissionsAsync(string userId);
    Task<bool> HasPermissionAsync(string permission);
    Task<string?> GetAccessTokenAsync();
    Task<bool> IsAuthenticatedAsync();
    Task SignOutAsync();
}

public class UserInfo
{
    public string? Id { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public string? TenantId { get; set; }
    public string? TenantName { get; set; }
    public DateTime? LastLogin { get; set; }
}