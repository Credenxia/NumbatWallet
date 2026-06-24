using FluentAssertions;
using NumbatWallet.Web.Admin.Authentication;

namespace NumbatWallet.Web.Admin.Tests.Authentication;

/// <summary>
/// The admin portal must run exactly one interactive-login mode. Development always uses the
/// local cookie login; outside Development, Credentry SSO is the sole login when enabled,
/// otherwise Azure AD. This guards the deployed test env from silently falling back to Azure AD
/// (which it has no app registration for) when Credentry is on.
/// </summary>
public class AdminAuthModeTests
{
    [Fact]
    public void Development_AlwaysUsesDevCookieLogin_EvenWhenCredentryEnabled()
    {
        AdminAuthentication.ResolveMode(isDevelopment: true, credentryEnabled: true)
            .Should().Be(AdminAuthMode.Development);
    }

    [Fact]
    public void Development_WithCredentryDisabled_UsesDevCookieLogin()
    {
        AdminAuthentication.ResolveMode(isDevelopment: true, credentryEnabled: false)
            .Should().Be(AdminAuthMode.Development);
    }

    [Fact]
    public void NonDevelopment_WithCredentryEnabled_UsesCredentrySso()
    {
        AdminAuthentication.ResolveMode(isDevelopment: false, credentryEnabled: true)
            .Should().Be(AdminAuthMode.CredentrySso);
    }

    [Fact]
    public void NonDevelopment_WithCredentryDisabled_FallsBackToAzureAd()
    {
        AdminAuthentication.ResolveMode(isDevelopment: false, credentryEnabled: false)
            .Should().Be(AdminAuthMode.AzureAd);
    }
}
