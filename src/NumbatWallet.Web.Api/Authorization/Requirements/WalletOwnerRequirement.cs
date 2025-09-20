using Microsoft.AspNetCore.Authorization;

namespace NumbatWallet.Web.Api.Authorization.Requirements;

public class WalletOwnerRequirement : IAuthorizationRequirement
{
    public bool AllowAdminAccess { get; }

    public WalletOwnerRequirement(bool allowAdminAccess = true)
    {
        AllowAdminAccess = allowAdminAccess;
    }
}