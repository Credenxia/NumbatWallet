using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NumbatWallet.Application.Services;
using Xunit;

namespace NumbatWallet.Application.Tests.Authentication;

public class HmacAccessTokenSignerTests
{
    private const string DevSecret = "ThisIsADevelopmentSecretKeyThatIs256BitsLong!!";

    private static IConfiguration ConfigWith(string? secret)
    {
        var mock = new Mock<IConfiguration>();
        mock.Setup(c => c["Jwt:Key"]).Returns((string?)null);
        mock.Setup(c => c["Jwt:SecretKey"]).Returns(secret);
        mock.Setup(c => c["Jwt:Issuer"]).Returns("https://numbatwallet.wa.gov.au");
        mock.Setup(c => c["Jwt:Audience"]).Returns("https://api.numbatwallet.wa.gov.au");
        return mock.Object;
    }

    [Fact]
    public void CreateToken_ProducesTokenValidatableWithItsOwnValidationKeys()
    {
        var signer = new HmacAccessTokenSigner(ConfigWith(DevSecret));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "person-1"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("tenant_id", "00000000-0000-0000-0000-000000000001")
        };

        var token = signer.CreateToken(claims, DateTimeOffset.UtcNow.AddMinutes(5));

        token.Should().NotBeNullOrWhiteSpace();
        signer.Algorithm.Should().Be("HS256");

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signer.GetValidationKeys(),
            ValidAlgorithms = new[] { signer.Algorithm },
            ValidateIssuer = true,
            ValidIssuer = signer.Issuer,
            ValidateAudience = true,
            ValidAudience = signer.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        }, out _);

        principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be("person-1");
        principal.IsInRole("Admin").Should().BeTrue();
    }

    [Fact]
    public void CreateToken_WithExplicitIssuerAndAudience_OverridesPlatformDefaults()
    {
        // The VP/VC overload: iss/aud are entity identifiers, not the API issuer/audience.
        var signer = new HmacAccessTokenSigner(ConfigWith(DevSecret));
        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-1);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        var token = signer.CreateToken(
            new[] { new Claim("nonce", "n-1") },
            issuer: "urn:uuid:11111111-1111-1111-1111-111111111111",
            audience: "verifier_42",
            notBefore: notBefore,
            expiresAt: expiresAt);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Issuer.Should().Be("urn:uuid:11111111-1111-1111-1111-111111111111");
        jwt.Audiences.Should().ContainSingle().Which.Should().Be("verifier_42");
        jwt.ValidFrom.Should().BeCloseTo(notBefore.UtcDateTime, TimeSpan.FromSeconds(2));
        jwt.ValidTo.Should().BeCloseTo(expiresAt.UtcDateTime, TimeSpan.FromSeconds(2));
        jwt.Claims.First(c => c.Type == "nonce").Value.Should().Be("n-1");

        // Still validates against the signer's own keys.
        new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signer.GetValidationKeys(),
            ValidAlgorithms = new[] { signer.Algorithm },
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true
        }, out _);
    }

    [Fact]
    public void CreateToken_WithNullExpiryAndAudience_OmitsThoseClaimsButKeepsNbf()
    {
        // A VC for a credential that never expires has no exp; a VC has no aud.
        // nbf must STILL be present (JwtSecurityToken drops it with a null expiry — the
        // signer compensates).
        var signer = new HmacAccessTokenSigner(ConfigWith(DevSecret));
        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);

        var token = signer.CreateToken(
            new[] { new Claim("vc_marker", "1") },
            issuer: "urn:uuid:22222222-2222-2222-2222-222222222222",
            audience: null,
            notBefore: notBefore,
            expiresAt: null);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().NotContain(c => c.Type == JwtRegisteredClaimNames.Exp);
        jwt.Claims.Should().NotContain(c => c.Type == JwtRegisteredClaimNames.Aud);
        jwt.ValidFrom.Should().BeCloseTo(notBefore.UtcDateTime, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void CreateToken_Throws_WhenSigningKeyMissing()
    {
        var signer = new HmacAccessTokenSigner(ConfigWith(null));

        var act = () => signer.CreateToken(Array.Empty<Claim>(), DateTimeOffset.UtcNow.AddMinutes(5));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*signing key is not configured*");
    }

    [Fact]
    public void TamperedToken_FailsValidation()
    {
        var signer = new HmacAccessTokenSigner(ConfigWith(DevSecret));
        var token = signer.CreateToken(new[] { new Claim(ClaimTypes.NameIdentifier, "person-1") },
            DateTimeOffset.UtcNow.AddMinutes(5));

        // Flip the last character of the signature segment.
        var tampered = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var act = () => handler.ValidateToken(tampered, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signer.GetValidationKeys(),
            ValidAlgorithms = new[] { signer.Algorithm },
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        }, out _);

        act.Should().Throw<SecurityTokenException>();
    }
}
