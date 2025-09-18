namespace NumbatWallet.SharedKernel.Interfaces;

public interface ITenantAware
{
    string TenantId { get; set; }
}