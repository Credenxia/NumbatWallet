using FluentAssertions;
using NumbatWallet.Web.Api.Authentication;
using Xunit;

namespace NumbatWallet.Web.Api.Tests.Authentication;

/// <summary>
/// Fail-fast validation rules for <see cref="CredentryOptions"/> (run at host start via
/// ValidateOnStart). The rules are environment-agnostic: only loopback authorities may be HTTP.
/// </summary>
public class CredentryOptionsValidatorTests
{
    private const string ValidCredentryTenant = "34c1955b-485e-4aab-bf5c-08488f3e80b5";
    private const string ValidNumbatTenant = "00000000-0000-0000-0000-000000000001";

    private static readonly CredentryOptionsValidator Validator = new();

    private static CredentryOptions Enabled(Action<CredentryOptions>? mutate = null)
    {
        var options = new CredentryOptions
        {
            Enabled = true,
            Authority = "https://tst.portal.credentry.com.au",
            TenantMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ValidCredentryTenant] = ValidNumbatTenant
            }
        };
        mutate?.Invoke(options);
        return options;
    }

    [Fact]
    public void Disabled_IsAlwaysValid_EvenWithGarbageConfig()
    {
        var options = new CredentryOptions
        {
            Enabled = false,
            Authority = null,
            TenantMap = new Dictionary<string, string>()
        };

        Validator.Validate(null, options).Succeeded.Should().BeTrue(
            "a disabled federation wires nothing, so its other fields are irrelevant");
    }

    [Fact]
    public void Enabled_WithHttpsAuthorityAndValidMap_IsValid()
    {
        Validator.Validate(null, Enabled()).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Enabled_WithLoopbackHttpAuthority_IsValid()
    {
        // The dev/stub IdP runs over loopback HTTP — must remain valid.
        var options = Enabled(o => o.Authority = "http://127.0.0.1:5144");

        Validator.Validate(null, options).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Enabled_WithLocalhostHttpAuthority_IsValid()
    {
        var options = Enabled(o => o.Authority = "http://localhost:5144");

        Validator.Validate(null, options).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Enabled_WithoutAuthority_Fails()
    {
        var result = Validator.Validate(null, Enabled(o => o.Authority = null));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Authority");
    }

    [Fact]
    public void Enabled_WithRelativeAuthority_Fails()
    {
        // No scheme → not an absolute URI.
        var result = Validator.Validate(null, Enabled(o => o.Authority = "credentry.example.com"));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("absolute");
    }

    [Fact]
    public void Enabled_WithNonHttpScheme_Fails()
    {
        // A bare "/path" parses as an absolute file:// URI on Unix — must still be rejected.
        var result = Validator.Validate(null, Enabled(o => o.Authority = "/connect"));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("http or https");
    }

    [Fact]
    public void Enabled_WithNonLoopbackHttpAuthority_Fails()
    {
        var result = Validator.Validate(null, Enabled(o => o.Authority = "http://tst.portal.credentry.com.au"));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("HTTPS");
    }

    [Fact]
    public void Enabled_WithEmptyTenantMap_Fails()
    {
        var result = Validator.Validate(null, Enabled(o => o.TenantMap = new Dictionary<string, string>()));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("TenantMap");
    }

    [Fact]
    public void Enabled_WithNonGuidTenantMapKey_Fails()
    {
        var result = Validator.Validate(null, Enabled(o => o.TenantMap = new Dictionary<string, string>
        {
            ["not-a-guid"] = ValidNumbatTenant
        }));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("not a valid GUID");
    }

    [Fact]
    public void Enabled_WithNonGuidTenantMapValue_Fails()
    {
        var result = Validator.Validate(null, Enabled(o => o.TenantMap = new Dictionary<string, string>
        {
            [ValidCredentryTenant] = "not-a-guid"
        }));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("not a valid GUID");
    }

    [Fact]
    public void Enabled_WithEmptyAudience_Fails()
    {
        var result = Validator.Validate(null, Enabled(o => o.Audience = ""));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Audience");
    }
}
