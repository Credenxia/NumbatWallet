using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Application.Commands.Presentations;
using NumbatWallet.Application.Commands.Presentations.Handlers;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Tests.Commands.Presentations;

public class VerifyPresentationCommandHandlerTests
{
    private const string Secret = "TestPresentationSecretKeyThatIs256BitsLong!!";
    private const string Issuer = "https://numbatwallet.wa.gov.au";
    private const string Audience = "https://api.numbatwallet.wa.gov.au";

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
            _configurationMock.Object,
            new Mock<ILogger<VerifyPresentationCommandHandler>>().Object);
    }

    private static string CreateToken(
        Guid presentationId,
        Guid credentialId,
        string secret = Secret,
        DateTimeOffset? expiresAt = null,
        string typ = "presentation",
        Dictionary<string, object>? disclosedClaims = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, presentationId.ToString()),
            new("typ", typ),
            new("credential_id", credentialId.ToString()),
            new("verifier_id", "verifier_123"),
            new("purpose", "Age verification"),
            new("disclosed_claims", JsonSerializer.Serialize(disclosedClaims ?? new Dictionary<string, object>()), JsonClaimValueTypes.Json)
        };

        var expiry = (expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(15)).UtcDateTime;
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: expiry.AddMinutes(-30),
            expires: expiry,
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
        var token = CreateToken(presentation.Id, credential.Id, disclosedClaims: disclosedClaims);

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
        var token = CreateToken(presentation.Id, credential.Id);

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
        // Arrange — token signed with a DIFFERENT key
        var (presentation, credential) = SetupValidPresentation();
        var token = CreateToken(presentation.Id, credential.Id,
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
        var token = CreateToken(presentation.Id, credential.Id,
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(-5));

        // Act
        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("expired");
    }

    [Fact]
    public async Task HandleAsync_NonPresentationToken_ReturnsInvalid()
    {
        // Arrange — valid signature but typ != presentation (e.g. an access token)
        var (presentation, credential) = SetupValidPresentation();
        var token = CreateToken(presentation.Id, credential.Id, typ: "access");

        // Act
        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("not a presentation token");
    }

    [Fact]
    public async Task HandleAsync_UnknownJti_ReturnsInvalid()
    {
        // Arrange — well-signed token but no presentation record for the jti
        var token = CreateToken(Guid.NewGuid(), Guid.NewGuid());
        _presentationRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Presentation?)null);

        // Act
        var result = await _handler.HandleAsync(new VerifyPresentationCommand(token));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("not found");
    }

    [Fact]
    public async Task HandleAsync_RevokedCredential_ReturnsInvalid()
    {
        // Arrange — credential revoked AFTER the presentation was created
        var (presentation, credential) = SetupValidPresentation();
        credential.Revoke("Compromised");
        var token = CreateToken(presentation.Id, credential.Id);

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
        var token = CreateToken(presentation.Id, credential.Id);

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
        var token = CreateToken(presentation.Id, credential.Id);

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
        _configurationMock.Setup(c => c["Jwt:SecretKey"]).Returns((string?)null);
        var token = CreateToken(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(new VerifyPresentationCommand(token)));
    }
}
