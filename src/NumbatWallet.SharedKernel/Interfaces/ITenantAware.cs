namespace NumbatWallet.SharedKernel.Interfaces;

public interface ITenantAware
{
    Guid TenantId { get; set; }
}