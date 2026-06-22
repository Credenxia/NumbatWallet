using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Web.Api.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace NumbatWallet.Web.Api.Tests.Authentication;

public class CredentryTokenInspectorTests
{
    private const string CredentryIssuer = "http://localhost:5144";

    private static string CreateJwt(string? issuer)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("UnitTestSigningKeyThatIsAtLeast256BitsLong!!"));
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: "numbatwallet-api",
            claims: [new Claim("sub", "test")],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public void IsCredentryToken_TokenIssuedByCredentry_ReturnsTrue()
    {
        var header = $"Bearer {CreateJwt(CredentryIssuer)}";

        CredentryTokenInspector.IsCredentryToken(header, CredentryIssuer).Should().BeTrue();
    }

    [Fact]
    public void IsCredentryToken_TrailingSlashDifference_ReturnsTrue()
    {
        var header = $"Bearer {CreateJwt(CredentryIssuer + "/")}";

        CredentryTokenInspector.IsCredentryToken(header, CredentryIssuer).Should().BeTrue();
    }

    [Fact]
    public void IsCredentryToken_TokenFromOtherIssuer_ReturnsFalse()
    {
        var header = $"Bearer {CreateJwt("https://login.microsoftonline.com/contoso/v2.0")}";

        CredentryTokenInspector.IsCredentryToken(header, CredentryIssuer).Should().BeFalse();
    }

    [Fact]
    public void IsCredentryToken_SelfIssuedNumbatToken_ReturnsFalse()
    {
        // The existing HS256 self-issued tokens use the NumbatWallet issuer — they must keep
        // flowing to the existing default scheme.
        var header = $"Bearer {CreateJwt("NumbatWallet")}";

        CredentryTokenInspector.IsCredentryToken(header, CredentryIssuer).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Bearer")]
    [InlineData("Bearer ")]
    [InlineData("Bearer not-a-jwt")]
    [InlineData("Bearer aaa.bbb")]
    [InlineData("Basic dXNlcjpwYXNz")]
    [InlineData("ApiKey something")]
    public void IsCredentryToken_MissingOrMalformedHeader_ReturnsFalse(string? header)
    {
        CredentryTokenInspector.IsCredentryToken(header, CredentryIssuer).Should().BeFalse();
    }

    [Fact]
    public void IsCredentryToken_NoConfiguredIssuer_ReturnsFalse()
    {
        var header = $"Bearer {CreateJwt(CredentryIssuer)}";

        CredentryTokenInspector.IsCredentryToken(header, null).Should().BeFalse();
        CredentryTokenInspector.IsCredentryToken(header, "").Should().BeFalse();
    }

    [Fact]
    public void IsCredentryToken_BearerPrefixIsCaseInsensitive_ReturnsTrue()
    {
        var header = $"bearer {CreateJwt(CredentryIssuer)}";

        CredentryTokenInspector.IsCredentryToken(header, CredentryIssuer).Should().BeTrue();
    }

    [Fact]
    public void IsCredentryToken_TokenWithoutIssuer_ReturnsFalse()
    {
        var header = $"Bearer {CreateJwt(null)}";

        CredentryTokenInspector.IsCredentryToken(header, CredentryIssuer).Should().BeFalse();
    }
}
