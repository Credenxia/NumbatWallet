using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

public class TenantServiceAdapter : SharedKernel.Interfaces.ITenantService
{
    private readonly ITenantService _applicationTenantService;

    public TenantServiceAdapter(ITenantService applicationTenantService)
    {
        _applicationTenantService = applicationTenantService;
    }

    // Local development convenience tenant. Only used when no tenant can be resolved
    // AND the app is running in Development. In any other environment an unresolved
    // tenant fails CLOSED (Guid.Empty) so EF query filters match no rows rather than
    // silently exposing a default tenant's data.
    private static readonly Guid DevDefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static bool IsDevelopment => string.Equals(
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
        "Development", StringComparison.OrdinalIgnoreCase);

    public Guid TenantId
    {
        get
        {
            var tenantIdString = _applicationTenantService.GetCurrentTenantId();
            if (!string.IsNullOrEmpty(tenantIdString) && Guid.TryParse(tenantIdString, out var tenantId))
            {
                return tenantId;
            }

            // Unresolved tenant: fail closed outside Development.
            return IsDevelopment ? DevDefaultTenantId : Guid.Empty;
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
