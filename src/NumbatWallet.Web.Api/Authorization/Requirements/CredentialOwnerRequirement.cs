using Microsoft.AspNetCore.Authorization;

namespace NumbatWallet.Web.Api.Authorization.Requirements;

public class CredentialOwnerRequirement : IAuthorizationRequirement
{
    public bool AllowIssuerAccess { get; }
    public bool AllowAdminAccess { get; }

    public CredentialOwnerRequirement(
        bool allowIssuerAccess = false,
        bool allowAdminAccess = true)
    {
        AllowIssuerAccess = allowIssuerAccess;
        AllowAdminAccess = allowAdminAccess;
    }
}