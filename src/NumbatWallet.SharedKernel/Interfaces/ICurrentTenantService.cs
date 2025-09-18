namespace NumbatWallet.SharedKernel.Interfaces;

public interface ICurrentTenantService
{
    string? TenantId { get; }
    string? TenantName { get; }
    bool IsMultiTenantContext { get; }
    Task<bool> SetTenantAsync(string tenantId);
}