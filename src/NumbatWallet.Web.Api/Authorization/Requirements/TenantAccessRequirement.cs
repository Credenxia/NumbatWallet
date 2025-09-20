using Microsoft.AspNetCore.Authorization;

namespace NumbatWallet.Web.Api.Authorization.Requirements;

public class TenantAccessRequirement : IAuthorizationRequirement
{
    public bool AllowAdminBypass { get; }

    public TenantAccessRequirement(bool allowAdminBypass = true)
    {
        AllowAdminBypass = allowAdminBypass;
    }
}