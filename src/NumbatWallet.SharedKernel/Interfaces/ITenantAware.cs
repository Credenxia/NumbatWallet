namespace NumbatWallet.SharedKernel.Interfaces;

public interface ITenantAware
{
    string TenantId { get; }
    void SetTenantId(string tenantId);
}