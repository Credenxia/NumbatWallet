using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Interfaces;

public interface ITenantService
{
    Task<TenantDto?> GetCurrentTenantAsync();
    Task<TenantDto?> GetTenantByIdAsync(string tenantId, CancellationToken cancellationToken = default);
    Task SetCurrentTenantAsync(string tenantId);
    Task ClearCurrentTenantAsync();
    Task<bool> ValidateTenantAsync(string tenantId);
    Task<IEnumerable<TenantDto>> GetAllTenantsAsync();
    Task<IEnumerable<TenantDto>> GetAllTenants(CancellationToken cancellationToken = default);
    string? GetCurrentTenantId();
    Task<TenantDto> CreateTenantAsync(CreateTenantDto dto, CancellationToken cancellationToken = default);
    Task<TenantDto> UpdateTenantAsync(string tenantId, UpdateTenantDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<bool> ActivateTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<bool> DeactivateTenantAsync(string tenantId, CancellationToken cancellationToken = default);
}

public interface IAuthenticationService
{
    Task<bool> IsUserActiveAsync(string userId);
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<bool> ValidateApiKeyAsync(string apiKey);
}
