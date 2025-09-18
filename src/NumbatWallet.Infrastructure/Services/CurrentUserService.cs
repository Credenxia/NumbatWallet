using Microsoft.AspNetCore.Http;
using NumbatWallet.SharedKernel.Interfaces;
using System.Security.Claims;

namespace NumbatWallet.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

    public string? UserName => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value
        ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name;

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<string> Roles
    {
        get
        {
            var roles = _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)
                .Select(c => c.Value) ?? Enumerable.Empty<string>();

            return roles;
        }
    }

    public IEnumerable<KeyValuePair<string, string>> Claims
    {
        get
        {
            var claims = _httpContextAccessor.HttpContext?.User?.Claims
                .Select(c => new KeyValuePair<string, string>(c.Type, c.Value))
                ?? Enumerable.Empty<KeyValuePair<string, string>>();

            return claims;
        }
    }
}