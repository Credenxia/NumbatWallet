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
