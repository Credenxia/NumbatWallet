namespace NumbatWallet.Web.Admin.Authentication;

/// <summary>The admin portal's interactive-login mode (exactly one is active at a time).</summary>
public enum AdminAuthMode
{
    /// <summary>Dev cookie login (/login) for seeded local accounts; Credentry optional.</summary>
    Development,

    /// <summary>"Sign in with Credentry" is the sole interactive login (deployed test/nonprod).</summary>
    CredentrySso,

    /// <summary>Azure AD (Entra) sign-in (future production).</summary>
    AzureAd
}

/// <summary>Pure, testable selection of the admin authentication mode (see <see cref="AdminAuthMode"/>).</summary>
public static class AdminAuthentication
{
    /// <summary>
    /// Development always wins (local accounts). Otherwise Credentry SSO is preferred when
    /// enabled, falling back to Azure AD. This is the single source of truth the host switches on.
    /// </summary>
    public static AdminAuthMode ResolveMode(bool isDevelopment, bool credentryEnabled) =>
        isDevelopment ? AdminAuthMode.Development
        : credentryEnabled ? AdminAuthMode.CredentrySso
        : AdminAuthMode.AzureAd;
}
