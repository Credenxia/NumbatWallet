using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.Commands.Credentials.Handlers;
using NumbatWallet.Application.Exceptions;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Tests.Commands.Credentials;

public class PresentCredentialCommandHandlerTests
{
    private readonly Mock<ICredentialRepository> _credentialRepositoryMock;
    private readonly Mock<IPresentationRepository> _presentationRepositoryMock;
    private readonly Mock<IVerificationService> _verificationServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<Application.Interfaces.IEventDispatcher> _eventDispatcherMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly PresentCredentialCommandHandler _handler;

    public PresentCredentialCommandHandlerTests()
    {
        _credentialRepositoryMock = new Mock<ICredentialRepository>();
        _presentationRepositoryMock = new Mock<IPresentationRepository>();
        _verificationServiceMock = new Mock<IVerificationService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _eventDispatcherMock = new Mock<Application.Interfaces.IEventDispatcher>();
        _configurationMock = new Mock<IConfiguration>();
        var loggerMock = new Mock<ILogger<PresentCredentialCommandHandler>>();
        _handler = new PresentCredentialCommandHandler(
            _credentialRepositoryMock.Object,
            _presentationRepositoryMock.Object,
            _verificationServiceMock.Object,
            _unitOfWorkMock.Object,
            _eventDispatcherMock.Object,
            _configurationMock.Object,
            loggerMock.Object);
    }

    private void SetupTokenService(string presentationToken = "token", string verificationUrl = "url")
    {
        _verificationServiceMock.Setup(x => x.CreatePresentationTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(presentationToken);

        _verificationServiceMock.Setup(x => x.CreateVerificationUrlAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(verificationUrl);
    }

    [Fact]
    public async Task HandleAsync_ValidCredential_ReturnsPresentationResult()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var verifierId = "verifier_123";
        var purpose = "Age Verification";
        var presentationToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
        var verificationUrl = "https://verify.wallet.wa.gov.au/abc123";

        var credentialData = new Dictionary<string, object>
        {
            { "fullName", "John Doe" },
            { "dateOfBirth", "1990-01-01" },
            { "licenseNumber", "DL123456" }
        };

        var credentialResult = Credential.Create(
            walletId,
            issuerId,
            "DriversLicense",
            JsonSerializer.Serialize(credentialData),
            "schema:driverslicense:1.0");

        var credential = credentialResult.Value;
        credential.Activate();

        var command = new PresentCredentialCommand(
            credentialId,
            verifierId,
            purpose,
            new List<string> { "dateOfBirth", "fullName" });

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        SetupTokenService(presentationToken, verificationUrl);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.PresentationToken.Should().Be(presentationToken);
        result.VerificationUrl.Should().Be(verificationUrl);
        result.DisclosedClaims.Should().HaveCount(2);
        result.DisclosedClaims.Should().ContainKey("dateOfBirth");
        result.DisclosedClaims.Should().ContainKey("fullName");
        result.DisclosedClaims.Should().NotContainKey("licenseNumber"); // Not disclosed

        _eventDispatcherMock.Verify(x => x.DispatchAsync(
            It.IsAny<CredentialPresentedEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PersistsPresentation_WithSameIdAsTokenJti()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var credential = CreateActiveCredential(
            new Dictionary<string, object> { { "fullName", "John Doe" }, { "dateOfBirth", "1990-01-01" } });

        var command = new PresentCredentialCommand(
            credentialId,
            "verifier_123",
            "Age Verification",
            new List<string> { "dateOfBirth" });

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        Guid tokenPresentationId = Guid.Empty;
        DateTimeOffset tokenExpiresAt = default;
        _verificationServiceMock.Setup(x => x.CreatePresentationTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Callback((Guid pid, Guid _, string _, string _, Dictionary<string, object> _, DateTimeOffset exp, CancellationToken _) =>
            {
                tokenPresentationId = pid;
                tokenExpiresAt = exp;
            })
            .ReturnsAsync("token");
        _verificationServiceMock.Setup(x => x.CreateVerificationUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("url");

        Presentation? persisted = null;
        _presentationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Presentation>(), It.IsAny<CancellationToken>()))
            .Callback((Presentation p, CancellationToken _) => persisted = p)
            .ReturnsAsync((Presentation p, CancellationToken _) => p);

        // Act
        await _handler.HandleAsync(command);

        // Assert — the persisted presentation IS the token's jti
        persisted.Should().NotBeNull();
        persisted!.Id.Should().Be(tokenPresentationId);
        persisted.CredentialId.Should().Be(credential.Id);
        persisted.WalletId.Should().Be(credential.WalletId);
        persisted.VerifierId.Should().Be("verifier_123");
        persisted.Purpose.Should().Be("Age Verification");
        persisted.ExpiresAt.Should().Be(tokenExpiresAt);
        persisted.DisclosedClaimsJson.Should().Contain("dateOfBirth");
        persisted.DisclosedClaimsJson.Should().NotContain("fullName");

        // Default lifetime is 15 minutes when not configured
        persisted.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ConfiguredLifetime_IsUsedForExpiry()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var credential = CreateActiveCredential(new Dictionary<string, object> { { "a", "b" } });

        _configurationMock.Setup(c => c["Presentation:TokenLifetimeMinutes"]).Returns("60");

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        SetupTokenService();

        Presentation? persisted = null;
        _presentationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Presentation>(), It.IsAny<CancellationToken>()))
            .Callback((Presentation p, CancellationToken _) => persisted = p)
            .ReturnsAsync((Presentation p, CancellationToken _) => p);

        // Act
        await _handler.HandleAsync(new PresentCredentialCommand(credentialId, "v", "purpose", null));

        // Assert
        persisted!.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(60), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task HandleAsync_NoSelectiveDisclosure_DiscloseAllClaims()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var credential = CreateActiveCredential(new Dictionary<string, object>
        {
            { "fullName", "Jane Smith" },
            { "employeeId", "EMP001" },
            { "department", "IT" },
            { "startDate", "2020-01-15" }
        });

        var command = new PresentCredentialCommand(
            credentialId,
            "verifier_456",
            "Employment Verification",
            null); // No selective disclosure

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        SetupTokenService();

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.DisclosedClaims.Should().HaveCount(4);
        result.DisclosedClaims.Should().ContainKeys("fullName", "employeeId", "department", "startDate");
    }

    [Fact]
    public async Task HandleAsync_CredentialNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new PresentCredentialCommand(
            Guid.NewGuid(),
            "verifier_789",
            "Verification",
            null);

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(command.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Credential)null!);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_InactiveCredential_ThrowsBusinessRuleException()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();

        var credentialResult = Credential.Create(
            walletId,
            issuerId,
            "StudentId",
            "{}",
            "schema:studentid:1.0");

        var credential = credentialResult.Value;
        credential.Suspend("Security review"); // Make credential inactive

        var command = new PresentCredentialCommand(
            credentialId,
            "verifier_abc",
            "Student Verification",
            null);

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.HandleAsync(command));
        exception.Message.Should().Contain("Cannot present inactive credential");
        _presentationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Presentation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ExpiredCredential_ThrowsBusinessRuleException()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();

        var credentialResult = Credential.Create(
            walletId,
            issuerId,
            "ProofOfAge",
            "{}",
            "schema:proofofage:1.0");

        var credential = credentialResult.Value;
        credential.Activate();
        credential.SetExpiry(DateTimeOffset.UtcNow.AddHours(-1)); // Expired 1 hour ago

        var command = new PresentCredentialCommand(
            credentialId,
            "verifier_xyz",
            "Age Check",
            null);

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.HandleAsync(command));
        exception.Message.Should().Contain("Cannot present expired credential");
        _presentationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Presentation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_EmitsPresentedEvent_WithCorrectData()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var verifierId = "verifier_test";
        var purpose = "Test Purpose";
        var credential = CreateActiveCredential(new Dictionary<string, object>());

        var command = new PresentCredentialCommand(
            credentialId,
            verifierId,
            purpose,
            null);

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        SetupTokenService();

        CredentialPresentedEvent? capturedEvent = null;
        _eventDispatcherMock.Setup(x => x.DispatchAsync(
                It.IsAny<IDomainEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback((IDomainEvent evt, CancellationToken _) =>
            {
                capturedEvent = evt as CredentialPresentedEvent;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.CredentialId.Should().Be(credential.Id);
        capturedEvent.WalletId.Should().Be(credential.WalletId);
        capturedEvent.VerifierId.Should().Be(verifierId);
        capturedEvent.Purpose.Should().Be(purpose);
        capturedEvent.PresentedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    private static Credential CreateActiveCredential(Dictionary<string, object> claims)
    {
        var credentialResult = Credential.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Custom",
            JsonSerializer.Serialize(claims),
            "schema:custom:1.0");

        var credential = credentialResult.Value;
        credential.Activate();
        return credential;
    }
}
