using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Application.Services;
using NumbatWallet.Infrastructure.Services;

namespace NumbatWallet.Infrastructure.Tests.Services;

public class JwtPresentationTokenServiceTests
{
    private const string Secret = "TestPresentationSecretKeyThatIs256BitsLong!!";

    private static Mock<IConfiguration> ConfigWithSecret()
    {
        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(c => c["Jwt:SecretKey"]).Returns(Secret);
        return configurationMock;
    }

    private static JwtPresentationTokenService CreateService(Mock<IConfiguration>? configurationMock = null)
    {
        configurationMock ??= ConfigWithSecret();
        return new JwtPresentationTokenService(
            new HmacAccessTokenSigner(configurationMock.Object),
            configurationMock.Object,
            new Mock<ILogger<JwtPresentationTokenService>>().Object);
    }

    private static PresentationTokenRequest CreateRequest(
        Guid? presentationId = null,
        DateTimeOffset? credentialExpiresAt = null,
        Dictionary<string, object>? disclosedClaims = null)
    {
        return new PresentationTokenRequest(
            PresentationId: presentationId ?? Guid.NewGuid(),
            CredentialId: Guid.NewGuid(),
            WalletId: Guid.NewGuid(),
            IssuerId: Guid.NewGuid(),
            CredentialType: "ProofOfAge",
            CredentialIssuedAt: DateTimeOffset.UtcNow.AddDays(-30),
            CredentialExpiresAt: credentialExpiresAt,
            VerifierId: "verifier_123",
            Purpose: "Age verification",
            DisclosedClaims: disclosedClaims ?? new Dictionary<string, object> { { "dateOfBirth", "1990-01-01" } },
            Nonce: "test-nonce-123",
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(15));
    }

    [Fact]
    public async Task CreatePresentationTokenAsync_EmitsW3cVpJwtWithRegisteredClaims()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();

        // Act
        var token = await service.CreatePresentationTokenAsync(request, CancellationToken.None);

        // Assert — registered claims per the VC Data Model JWT mapping
        var vpJwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        vpJwt.Issuer.Should().Be($"urn:uuid:{request.WalletId}");
        vpJwt.Subject.Should().Be($"urn:uuid:{request.WalletId}");
        vpJwt.Audiences.Should().ContainSingle().Which.Should().Be("verifier_123");
        // jti stays the bare presentation Guid — it is the persisted record's Id (public contract).
        vpJwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value
            .Should().Be(request.PresentationId.ToString());
        vpJwt.Claims.First(c => c.Type == "nonce").Value.Should().Be("test-nonce-123");
        vpJwt.Claims.First(c => c.Type == "purpose").Value.Should().Be("Age verification");
        vpJwt.ValidTo.Should().BeCloseTo(request.ExpiresAt.UtcDateTime, TimeSpan.FromSeconds(2));
        vpJwt.ValidFrom.Should().BeCloseTo(DateTimeOffset.UtcNow.UtcDateTime, TimeSpan.FromSeconds(5));
        vpJwt.SignatureAlgorithm.Should().Be("HS256");
    }

    [Fact]
    public async Task CreatePresentationTokenAsync_VpClaimHasW3cStructureWithEmbeddedVcJwt()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();

        // Act
        var token = await service.CreatePresentationTokenAsync(request, CancellationToken.None);

        // Assert — vp claim is the W3C VerifiablePresentation object
        var vpJwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var vp = JsonSerializer.Deserialize<JsonElement>(
            vpJwt.Claims.First(c => c.Type == "vp").Value);

        vp.GetProperty("@context").EnumerateArray().Select(e => e.GetString())
            .Should().Contain("https://www.w3.org/2018/credentials/v1");
        vp.GetProperty("type").EnumerateArray().Select(e => e.GetString())
            .Should().Contain("VerifiablePresentation");

        var vcArray = vp.GetProperty("verifiableCredential");
        vcArray.GetArrayLength().Should().Be(1);
        var vcJwtString = vcArray[0].GetString();
        vcJwtString.Should().NotBeNullOrWhiteSpace();
        vcJwtString!.Split('.').Should().HaveCount(3, "the embedded credential must be a compact JWS");
    }

    [Fact]
    public async Task CreatePresentationTokenAsync_EmbeddedVcIsW3cCredentialWithOnlyDisclosedClaims()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest(
            credentialExpiresAt: DateTimeOffset.UtcNow.AddYears(1),
            disclosedClaims: new Dictionary<string, object> { { "dateOfBirth", "1990-01-01" } });

        // Act
        var token = await service.CreatePresentationTokenAsync(request, CancellationToken.None);

        // Assert
        var vpJwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var vp = JsonSerializer.Deserialize<JsonElement>(vpJwt.Claims.First(c => c.Type == "vp").Value);
        var vcJwt = new JwtSecurityTokenHandler().ReadJwtToken(
            vp.GetProperty("verifiableCredential")[0].GetString());

        // Registered claims: issuer entity, holder subject, credential id, credential validity.
        vcJwt.Issuer.Should().Be($"urn:uuid:{request.IssuerId}");
        vcJwt.Subject.Should().Be($"urn:uuid:{request.WalletId}");
        vcJwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value
            .Should().Be($"urn:uuid:{request.CredentialId}");
        vcJwt.ValidFrom.Should().BeCloseTo(request.CredentialIssuedAt.UtcDateTime, TimeSpan.FromSeconds(2));
        vcJwt.ValidTo.Should().BeCloseTo(request.CredentialExpiresAt!.Value.UtcDateTime, TimeSpan.FromSeconds(2));

        // vc claim: W3C VerifiableCredential whose credentialSubject carries ONLY disclosed claims.
        var vc = JsonSerializer.Deserialize<JsonElement>(vcJwt.Claims.First(c => c.Type == "vc").Value);
        vc.GetProperty("@context").EnumerateArray().Select(e => e.GetString())
            .Should().Contain("https://www.w3.org/2018/credentials/v1");
        vc.GetProperty("type").EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo("VerifiableCredential", "ProofOfAge");

        var subject = vc.GetProperty("credentialSubject");
        subject.GetProperty("id").GetString().Should().Be($"urn:uuid:{request.WalletId}");
        subject.GetProperty("dateOfBirth").GetString().Should().Be("1990-01-01");
        subject.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("dateOfBirth", "id");
    }

    [Fact]
    public async Task CreatePresentationTokenAsync_CredentialWithoutExpiry_OmitsVcExp()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest(credentialExpiresAt: null);

        // Act
        var token = await service.CreatePresentationTokenAsync(request, CancellationToken.None);

        // Assert
        var vpJwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var vp = JsonSerializer.Deserialize<JsonElement>(vpJwt.Claims.First(c => c.Type == "vp").Value);
        var vcJwt = new JwtSecurityTokenHandler().ReadJwtToken(
            vp.GetProperty("verifiableCredential")[0].GetString());

        vcJwt.Claims.Should().NotContain(c => c.Type == JwtRegisteredClaimNames.Exp);
        // nbf (issuanceDate mapping) must still be present.
        vcJwt.ValidFrom.Should().BeCloseTo(request.CredentialIssuedAt.UtcDateTime, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreatePresentationTokenAsync_BothTokensVerifiableWithSignerKeys()
    {
        // Arrange — the VP and embedded VC must validate against IAccessTokenSigner's keys,
        // proving the verify handler (which reads the same keys) can validate them.
        var configurationMock = ConfigWithSecret();
        var signer = new HmacAccessTokenSigner(configurationMock.Object);
        var service = CreateService(configurationMock);
        var request = CreateRequest();

        var token = await service.CreatePresentationTokenAsync(request, CancellationToken.None);

        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signer.GetValidationKeys(),
            ValidAlgorithms = new[] { signer.Algorithm },
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            RequireExpirationTime = false
        };

        // Act & Assert — neither throws
        var handler = new JwtSecurityTokenHandler();
        handler.ValidateToken(token, parameters, out _);

        var vpJwt = handler.ReadJwtToken(token);
        var vp = JsonSerializer.Deserialize<JsonElement>(vpJwt.Claims.First(c => c.Type == "vp").Value);
        handler.ValidateToken(vp.GetProperty("verifiableCredential")[0].GetString(), parameters, out _);
    }

    [Fact]
    public async Task CreatePresentationTokenAsync_WithoutSecret_Throws()
    {
        // Arrange — no Jwt:SecretKey configured: the signer must fail fast.
        var service = CreateService(new Mock<IConfiguration>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreatePresentationTokenAsync(CreateRequest(), CancellationToken.None));
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
        var configurationMock = ConfigWithSecret();
        configurationMock.Setup(c => c["Presentation:VerificationBaseUrl"]).Returns("https://verify.example.com/v/");
        var service = CreateService(configurationMock);

        // Act
        var url = await service.CreateVerificationUrlAsync("tok", CancellationToken.None);

        // Assert
        url.Should().Be("https://verify.example.com/v/tok");
    }
}
