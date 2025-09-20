using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

public class TenantServiceAdapter : SharedKernel.Interfaces.ITenantService
{
    private readonly ITenantService _applicationTenantService;

    public TenantServiceAdapter(ITenantService applicationTenantService)
    {
        _applicationTenantService = applicationTenantService;
    }

    public Guid TenantId
    {
        get
        {
            var tenantIdString = _applicationTenantService.GetCurrentTenantId();
            if (string.IsNullOrEmpty(tenantIdString))
            {
                // Return default tenant for development
                return Guid.Parse("00000000-0000-0000-0000-000000000001");
            }
            return Guid.TryParse(tenantIdString, out var tenantId) 
                ? tenantId 
                : Guid.Parse("00000000-0000-0000-0000-000000000001");
        }
    }

    public string TenantName
    {
        get
        {
            var currentTenant = _applicationTenantService.GetCurrentTenantAsync().GetAwaiter().GetResult();
            return currentTenant?.Name ?? "Default Tenant";
        }
    }
}