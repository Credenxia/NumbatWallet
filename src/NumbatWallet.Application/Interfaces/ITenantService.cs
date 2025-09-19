using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Interfaces;

public interface ITenantService
{
    Task<TenantDto?> GetCurrentTenantAsync();
    Task<TenantDto?> GetTenantByIdAsync(string tenantId);
    Task SetCurrentTenantAsync(string tenantId);
    Task ClearCurrentTenantAsync();
    Task<bool> ValidateTenantAsync(string tenantId);
    Task<IEnumerable<TenantDto>> GetAllTenantsAsync();
    string? GetCurrentTenantId();
}

public interface IAuthenticationService
{
    Task<bool> IsUserActiveAsync(string userId);
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<bool> ValidateApiKeyAsync(string apiKey);
}