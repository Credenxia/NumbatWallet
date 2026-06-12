using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NumbatWallet.Infrastructure.Services;

namespace NumbatWallet.Infrastructure.Tests.Services;

public class JwtPresentationTokenServiceTests
{
    private const string Secret = "TestPresentationSecretKeyThatIs256BitsLong!!";

    private static JwtPresentationTokenService CreateService(Mock<IConfiguration>? configurationMock = null)
    {
        if (configurationMock is null)
        {
            configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(c => c["Jwt:SecretKey"]).Returns(Secret);
        }

        return new JwtPresentationTokenService(
            configurationMock.Object,
            new Mock<ILogger<JwtPresentationTokenService>>().Object);
    }

    [Fact]
    public async Task CreatePresentationTokenAsync_CreatesSignedJwtWithExpectedClaims()
    {
        // Arrange
        var service = CreateService();
        var presentationId = Guid.NewGuid();
        var credentialId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        var disclosedClaims = new Dictionary<string, object> { { "dateOfBirth", "1990-01-01" } };

        // Act
        var token = await service.CreatePresentationTokenAsync(
            presentationId, credentialId, "verifier_123", "Age verification",
            disclosedClaims, expiresAt, CancellationToken.None);

        // Assert — real, parseable JWT (not a mock-presentation-... string)
        token.Should().NotStartWith("mock-");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value
            .Should().Be(presentationId.ToString());
        jwt.Claims.First(c => c.Type == "typ").Value.Should().Be("presentation");
        jwt.Claims.First(c => c.Type == "credential_id").Value.Should().Be(credentialId.ToString());
        jwt.Claims.First(c => c.Type == "verifier_id").Value.Should().Be("verifier_123");
        jwt.Claims.First(c => c.Type == "purpose").Value.Should().Be("Age verification");

        var disclosed = JsonSerializer.Deserialize<Dictionary<string, object>>(
            jwt.Claims.First(c => c.Type == "disclosed_claims").Value);
        disclosed.Should().ContainKey("dateOfBirth");

        jwt.ValidTo.Should().BeCloseTo(expiresAt.UtcDateTime, TimeSpan.FromSeconds(2));
        jwt.SignatureAlgorithm.Should().Be("HS256");
        jwt.Issuer.Should().Be("https://numbatwallet.wa.gov.au");
        jwt.Audiences.Should().Contain("https://api.numbatwallet.wa.gov.au");
    }

    [Fact]
    public async Task CreatePresentationTokenAsync_WithoutSecret_Throws()
    {
        // Arrange — no Jwt:SecretKey configured
        var service = CreateService(new Mock<IConfiguration>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreatePresentationTokenAsync(
                Guid.NewGuid(), Guid.NewGuid(), "v", "p",
                new Dictionary<string, object>(), DateTimeOffset.UtcNow.AddMinutes(15),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateVerificationUrlAsync_DefaultBaseUrl_AppendsToken()
    {
        // Arrange
        var service = CreateService();

        // Act
        var url = await service.CreateVerificationUrlAsync("the-token", CancellationToken.None);

        // Assert
        url.Should().Be("https://wallet.numbatwallet.com/verify/the-token");
    }

    [Fact]
    public async Task CreateVerificationUrlAsync_ConfiguredBaseUrl_IsUsed()
    {
        // Arrange
        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(c => c["Jwt:SecretKey"]).Returns(Secret);
        configurationMock.Setup(c => c["Presentation:VerificationBaseUrl"]).Returns("https://verify.example.com/v/");
        var service = CreateService(configurationMock);

        // Act
        var url = await service.CreateVerificationUrlAsync("tok", CancellationToken.None);

        // Assert
        url.Should().Be("https://verify.example.com/v/tok");
    }
}
