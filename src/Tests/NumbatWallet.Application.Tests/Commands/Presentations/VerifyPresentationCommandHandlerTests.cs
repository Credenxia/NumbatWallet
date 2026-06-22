using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Application.Commands.Presentations;
using NumbatWallet.Application.Commands.Presentations.Handlers;
using NumbatWallet.Application.Services;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Tests.Commands.Presentations;

public class VerifyPresentationCommandHandlerTests
{
    private const string Secret = "TestPresentationSecretKeyThatIs256BitsLong!!";
    private const string W3cContext = "https://www.w3.org/2018/credentials/v1";

    private readonly Mock<IPresentationRepository> _presentationRepositoryMock;
    private readonly Mock<ICredentialRepository> _credentialRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<Application.Interfaces.IEventDispatcher> _eventDispatcherMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly VerifyPresentationCommandHandler _handler;

    public VerifyPresentationCommandHandlerTests()
    {
        _presentationRepositoryMock = new Mock<IPresentationRepository>();
        _credentialRepositoryMock = new Mock<ICredentialRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _eventDispatcherMock = new Mock<Application.Interfaces.IEventDispatcher>();
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["Jwt:SecretKey"]).Returns(Secret);

        _handler = new VerifyPresentationCommandHandler(
            _presentationRepositoryMock.Object,
            _credentialRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _eventDispatcherMock.Object,
            new HmacAccessTokenSigner(_configurationMock.Object),
            new Mock<ILogger<VerifyPresentationCommandHandler>>().Object);
    }

    /// <summary>
    /// Mints a W3C VP-JWT mirroring Infrastructure.JwtPresentationTokenService, with knobs to
    /// corrupt each layer so every verification failure mode can be exercised.
    /// </summary>
    private static string CreateVpToken(
        Guid presentationId,
        Guid credentialId,
        Guid? walletId = null,
        string verifierId = "verifier_123",
        string secret = Secret,
        string vcSecret = Secret,
        DateTimeOffset? expiresAt = null,
        string? nonce = "nonce-abc",
        bool omitVpClaim = false,
        string vpContext = W3cContext,
        string vpType = "VerifiablePresentation",
        bool emptyVcArray = false,
        bool omitVcClaim = false,
        string vcType = "VerifiableCredential",
        Guid? vcJtiOverride = null,
        Dictionary<string, object>? disclosedClaims = null)
    {
        var holder = $"urn:uuid:{walletId ?? Guid.NewGuid()}";
        disclosedClaims ??= new Dictionary<string, object> { { "dateOfBirth", "1990-01-01" } };

        // --- Embedded VC-JWT ---
        var credentialSubject = new Dictionary<string, object>(disclosedClaims) { ["id"] = holder };
        var vc = new Dictionary<string, object>
        {
            ["@context"] = new[] { W3cContext },
            ["type"] = new[] { vcType, "ProofOfAge" },
            ["credentialSubject"] = credentialSubject
        };

        var vcClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, $"urn:uuid:{vcJtiOverride ?? credentialId}"),
            new(JwtRegisteredClaimNames.Sub, holder)
        };
        if (!omitVcClaim)
        {
            vcClaims.Add(new Claim("vc", JsonSerializer.Serialize(vc), JsonClaimValueTypes.Json));
        }

        var vcJwt = Sign(vcClaims, issuer: $"urn:uuid:{Guid.NewGuid()}", audience: null,
            notBefore: DateTimeOffset.UtcNow.AddDays(-1), expiresAt: DateTimeOffset.UtcNow.AddYears(1),
            secret: vcSecret);

        // --- VP-JWT ---
        var vp = new Dictionary<string, object>
        {
            ["@context"] = new[] { vpContext },
            ["type"] = new[] { vpType },
            ["verifiableCredential"] = emptyVcArray ? Array.Empty<string>() : new[] { vcJwt }
        };

        var vpClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, presentationId.ToString()),
            new(JwtRegisteredClaimNames.Sub, holder),
            new("purpose", "Age verification")
        };
        if (nonce is not null)
        {
            vpClaims.Add(new Claim("nonce", nonce));
        }
        if (!omitVpClaim)
        {
            vpClaims.Add(new Claim("vp", JsonSerializer.Serialize(vp), JsonClaimValueTypes.Json));
        }

        var expiry = expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(15);
        return Sign(vpClaims, issuer: holder, audience: verifierId,
            notBefore: expiry.AddMinutes(-30), expiresAt: expiry, secret: secret);
    }

    private static string Sign(
        IEnumerable<Claim> claims, string issuer, string? audience,
        DateTimeOffset notBefore, DateTimeOffset expiresAt, string secret)
    {
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: notBefore.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private (Presentation Presentation, Credential Credential) SetupValidPresentation(
        Dictionary<string, object>? disclosedClaims = null)
    {
        disclosedClaims ??= new Dictionary<string, object> { { "dateOfBirth", "1990-01-01" } };

        var credentialResult = Credential.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ProofOfAge",
            JsonSerializer.Serialize(disclosedClaims),
            "schema:proofofage:1.0");
        var credential = credentialResult.Value;
        credential.Activate();

        var presentationResult = Presentation.Create(
            credential.Id,
            credential.WalletId,
            "verifier_123",
            "Age verification",
            JsonSerializer.Serialize(disclosedClaims),
            DateTimeOffset.UtcNow.AddMinutes(15));
        var presentation = presentationResult.Value;

        _presentationRepositoryMock.Setup(x => x.GetByIdAsync(presentation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(presentation);
        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(credential.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        return (presentation, credential);
    }

    [Fact]
    public async Task HandleAsync_ValidToken_ReturnsValidResultWithDisclosedClaims()
    {
        // Arrange
        var disclosedClaims = new Dictionary<string, object>
        {
            { "dateOfBirth", "1990-01-01" },
            { "fullName", "John Doe" }
        };
        var (presentation, credential) = SetupValidPresentation(disclosedClaims);
        var token = CreateVpToken(presentation.Id, credential.Id,
            walletId: credential.WalletId, disclosedClaims: disclosedClaims);

        // Act
        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        // Assert
        result.IsValid.Should().BeTrue();
        result.Reason.Should().BeNull();
        result.PresentationId.Should().Be(presentation.Id);
        result.CredentialId.Should().Be(credential.Id);
        result.CredentialType.Should().Be("ProofOfAge");
        result.DisclosedClaims.Should().NotBeNull();
        result.DisclosedClaims!.Keys.Should().BeEquivalentTo("dateOfBirth", "fullName");
        result.PresentedAt.Should().NotBeNull();
        result.VerifiedAt.Should().NotBeNull();

        presentation.Status.Should().Be(PresentationStatus.Verified);
        presentation.VerificationCount.Should().Be(1);

        _presentationRepositoryMock.Verify(x => x.UpdateAsync(presentation, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventDispatcherMock.Verify(x => x.DispatchAsync(
            It.Is<PresentationVerifiedEvent>(e => e.PresentationId == presentation.Id && e.VerificationResult),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_VerifyTwice_BothSucceedAndCountIncrements()
    {
        // Arrange
        var (presentation, credential) = SetupValidPresentation();
        var token = CreateVpToken(presentation.Id, credential.Id);

        // Act
        var first = await _handler.HandleAsync(new VerifyPresentationCommand(token));
        var second = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        // Assert — re-verification within the token lifetime is allowed
        first.IsValid.Should().BeTrue();
        second.IsValid.Should().BeTrue();
        presentation.VerificationCount.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_GarbageToken_ReturnsInvalidWithoutThrowing()
    {
        // Act
        var result = await _handler.HandleAsync(new VerifyPresentationCommand("not-a-jwt-at-all"));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("invalid");
        _presentationRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_EmptyToken_ReturnsInvalid()
    {
        var result = await _handler.HandleAsync(new VerifyPresentationCommand("  "));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_BadSignature_ReturnsInvalid()
    {
        // Arrange — VP signed with a DIFFERENT key
        var (presentation, credential) = SetupValidPresentation();
        var token = CreateVpToken(presentation.Id, credential.Id,
            secret: "ADifferentSecretKeyThatIsAlso256BitsLong!!!!");

        // Act
        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("invalid");
        presentation.Status.Should().Be(PresentationStatus.Pending);
    }

    [Fact]
    public async Task HandleAsync_ExpiredToken_ReturnsInvalid()
    {
        // Arrange — token expired 5 minutes ago (beyond clock skew)
        var (presentation, credential) = SetupValidPresentation();
        var token = CreateVpToken(presentation.Id, credential.Id,
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(-5));

        // Act
        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("expired");
    }

    [Fact]
    public async Task HandleAsync_TokenWithoutVpClaim_ReturnsInvalid()
    {
        // Arrange — well-signed JWT that is NOT a VP (e.g. an access token)
        var (presentation, credential) = SetupValidPresentation();
        var token = CreateVpToken(presentation.Id, credential.Id, omitVpClaim: true);

        // Act
        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("missing vp claim");
    }

    [Fact]
    public async Task HandleAsync_WrongVpContext_ReturnsInvalid()
    {
        var (presentation, credential) = SetupValidPresentation();
        var token = CreateVpToken(presentation.Id, credential.Id,
            vpContext: "https://example.com/not-the-w3c-context");

        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("@context");
    }

    [Fact]
    public async Task HandleAsync_WrongVpType_ReturnsInvalid()
    {
        var (presentation, credential) = SetupValidPresentation();
        var token = CreateVpToken(presentation.Id, credential.Id, vpType: "SomethingElse");

        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("VerifiablePresentation");
    }

    [Fact]
    public async Task HandleAsync_NoEmbeddedCredential_ReturnsInvalid()
    {
        var (presentation, credential) = SetupValidPresentation();
        var token = CreateVpToken(presentation.Id, credential.Id, emptyVcArray: true);

        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("no verifiable credential");
    }

    [Fact]
    public async Task HandleAsync_MissingNonce_ReturnsInvalid()
    {
        var (presentation, credential) = SetupValidPresentation();
        var token = CreateVpToken(presentation.Id, credential.Id, nonce: null);

        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("nonce");
    }

    [Fact]
    public async Task HandleAsync_UnknownJti_ReturnsInvalid()
    {
        // Arrange — well-signed token but no presentation record for the jti
        var token = CreateVpToken(Guid.NewGuid(), Guid.NewGuid());
        _presentationRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Presentation?)null);

        // Act
        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("not found");
    }

    [Fact]
    public async Task HandleAsync_AudienceMismatch_ReturnsInvalid()
    {
        // Arrange — token minted for a different verifier than the persisted presentation
        var (presentation, credential) = SetupValidPresentation();
        var token = CreateVpToken(presentation.Id, credential.Id, verifierId: "someone_else");

        // Act
        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("audience");
    }

    [Fact]
    public async Task HandleAsync_EmbeddedVcBadSignature_ReturnsInvalid()
    {
        // Arrange — VP fine, but embedded VC signed with a different key
        var (presentation, credential) = SetupValidPresentation();
        var token = CreateVpToken(presentation.Id, credential.Id,
            vcSecret: "ADifferentSecretKeyThatIsAlso256BitsLong!!!!");

        // Act
        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("Embedded credential");
        result.Reason.Should().Contain("invalid");
    }

    [Fact]
    public async Task HandleAsync_EmbeddedTokenWithoutVcClaim_ReturnsInvalid()
    {
        var (presentation, credential) = SetupValidPresentation();
        var token = CreateVpToken(presentation.Id, credential.Id, omitVcClaim: true);

        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("missing vc claim");
    }

    [Fact]
    public async Task HandleAsync_WrongVcType_ReturnsInvalid()
    {
        var (presentation, credential) = SetupValidPresentation();
        var token = CreateVpToken(presentation.Id, credential.Id, vcType: "NotACredential");

        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("VerifiableCredential");
    }

    [Fact]
    public async Task HandleAsync_EmbeddedVcForDifferentCredential_ReturnsInvalid()
    {
        // Arrange — VC↔presentation consistency: embedded VC carries another credential's id
        var (presentation, credential) = SetupValidPresentation();
        var token = CreateVpToken(presentation.Id, credential.Id, vcJtiOverride: Guid.NewGuid());

        // Act
        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("does not match the presented credential");
    }

    [Fact]
    public async Task HandleAsync_RevokedCredential_ReturnsInvalid()
    {
        // Arrange — credential revoked AFTER the presentation was created
        var (presentation, credential) = SetupValidPresentation();
        credential.Revoke("Compromised");
        var token = CreateVpToken(presentation.Id, credential.Id);

        // Act
        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("revoked");
        presentation.Status.Should().Be(PresentationStatus.Pending);
        _eventDispatcherMock.Verify(x => x.DispatchAsync(
            It.IsAny<PresentationVerifiedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ExpiredCredential_ReturnsInvalid()
    {
        // Arrange — credential expired AFTER the presentation was created
        var (presentation, credential) = SetupValidPresentation();
        credential.SetExpiry(DateTimeOffset.UtcNow.AddMilliseconds(-1));
        var token = CreateVpToken(presentation.Id, credential.Id);

        // Act
        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("expired");
    }

    [Fact]
    public async Task HandleAsync_MissingCredential_ReturnsInvalid()
    {
        // Arrange
        var (presentation, credential) = SetupValidPresentation();
        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(credential.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Credential?)null);
        var token = CreateVpToken(presentation.Id, credential.Id);

        // Act
        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("no longer exists");
    }

    [Fact]
    public async Task HandleAsync_MissingSecret_ThrowsInvalidOperation()
    {
        // Arrange — configuration error is NOT a normal verification failure
        var token = CreateVpToken(Guid.NewGuid(), Guid.NewGuid());
        _configurationMock.Setup(c => c["Jwt:SecretKey"]).Returns((string?)null);
        _configurationMock.Setup(c => c["Jwt:Key"]).Returns((string?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(new VerifyPresentationCommand(token)));
    }
}
