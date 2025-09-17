namespace NumbatWallet.SharedKernel.Interfaces;

public interface ITenantService
{
    Guid TenantId { get; }
    string TenantName { get; }
}