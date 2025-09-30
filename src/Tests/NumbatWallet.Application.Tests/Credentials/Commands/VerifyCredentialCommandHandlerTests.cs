using Moq;
using FluentAssertions;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.Commands.Credentials.Handlers;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Domain.Aggregates;
using Microsoft.Extensions.Logging;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Tests.Credentials.Commands;

public class VerifyCredentialCommandHandlerTests
{
    private readonly Mock<ICredentialRepository> _credentialRepositoryMock;
    private readonly Mock<ILogger<VerifyCredentialCommandHandler>> _loggerMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IJwtSigningService> _jwtSigningServiceMock;
    private readonly Mock<IWalletRepository> _walletRepositoryMock;
    private readonly Mock<IPersonRepository> _personRepositoryMock;
    private readonly VerifyCredentialCommandHandler _handler;

    public VerifyCredentialCommandHandlerTests()
    {
        _credentialRepositoryMock = new Mock<ICredentialRepository>();
        _loggerMock = new Mock<ILogger<VerifyCredentialCommandHandler>>();
        _cacheServiceMock = new Mock<ICacheService>();
        _jwtSigningServiceMock = new Mock<IJwtSigningService>();
        _walletRepositoryMock = new Mock<IWalletRepository>();
        _personRepositoryMock = new Mock<IPersonRepository>();

        _handler = new VerifyCredentialCommandHandler(
            _credentialRepositoryMock.Object,
            _loggerMock.Object,
            _cacheServiceMock.Object,
            _jwtSigningServiceMock.Object,
            _walletRepositoryMock.Object,
            _personRepositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_CredentialNotFound_ReturnsInvalidResult()
    {
        // Arrange
        var command = new VerifyCredentialCommand
        {
            CredentialId = Guid.NewGuid().ToString()
        };

        _cacheServiceMock.Setup(x => x.GetAsync<VerificationResultDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VerificationResultDto?)null);
        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Credential?)null);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Credential not found");
    }

    [Fact]
    public async Task HandleAsync_ValidJwtCredential_VerifiesSignature()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var validJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";

        var credentialResult = Credential.Create(
            walletId,
            issuerId,
            "DriversLicense",
            validJwt,
            "schema:driverslicense:1.0");

        var credential = credentialResult.Value;
        credential.SetTenantId("test-tenant");
        credential.Activate();

        var command = new VerifyCredentialCommand
        {
            CredentialId = credential.Id.ToString()
        };

        _cacheServiceMock.Setup(x => x.GetAsync<VerificationResultDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VerificationResultDto?)null);
        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _jwtSigningServiceMock.Setup(x => x.VerifyCredentialAsync(validJwt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _cacheServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<VerificationResultDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Checks.Should().NotBeNull();
        result.Checks!.Signature.Should().BeTrue();
        _jwtSigningServiceMock.Verify(x => x.VerifyCredentialAsync(validJwt, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_InvalidJwtSignature_ReturnsInvalidResult()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var invalidJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.INVALIDSIGNATURE";

        var credentialResult = Credential.Create(
            walletId,
            issuerId,
            "DriversLicense",
            invalidJwt,
            "schema:driverslicense:1.0");

        var credential = credentialResult.Value;
        credential.SetTenantId("test-tenant");
        credential.Activate();

        var command = new VerifyCredentialCommand
        {
            CredentialId = credential.Id.ToString()
        };

        _cacheServiceMock.Setup(x => x.GetAsync<VerificationResultDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VerificationResultDto?)null);
        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _jwtSigningServiceMock.Setup(x => x.VerifyCredentialAsync(invalidJwt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Checks.Should().NotBeNull();
        result.Checks!.Signature.Should().BeFalse();
        result.ErrorMessage.Should().Contain("signature verification failed");
        _jwtSigningServiceMock.Verify(x => x.VerifyCredentialAsync(invalidJwt, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NonJwtCredential_SkipsSignatureVerification()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var jsonData = "{\"type\":\"DriversLicense\",\"holder\":\"John Doe\"}";

        var credentialResult = Credential.Create(
            walletId,
            issuerId,
            "DriversLicense",
            jsonData,
            "schema:driverslicense:1.0");

        var credential = credentialResult.Value;
        credential.SetTenantId("test-tenant");
        credential.Activate();

        var command = new VerifyCredentialCommand
        {
            CredentialId = credential.Id.ToString()
        };

        _cacheServiceMock.Setup(x => x.GetAsync<VerificationResultDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VerificationResultDto?)null);
        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _cacheServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<VerificationResultDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Checks.Should().NotBeNull();
        result.Checks!.Signature.Should().BeTrue(); // Legacy credential, no signature verification
        _jwtSigningServiceMock.Verify(x => x.VerifyCredentialAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ExpiredCredential_ReturnsInvalidResult()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();

        var credentialResult = Credential.Create(
            walletId,
            issuerId,
            "DriversLicense",
            "{\"type\":\"DriversLicense\"}",
            "schema:driverslicense:1.0");

        var credential = credentialResult.Value;
        credential.SetTenantId("test-tenant");
        credential.Activate();
        credential.SetExpiry(DateTimeOffset.UtcNow.AddDays(-1)); // Expired yesterday

        var command = new VerifyCredentialCommand
        {
            CredentialId = credential.Id.ToString()
        };

        _cacheServiceMock.Setup(x => x.GetAsync<VerificationResultDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VerificationResultDto?)null);
        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Checks.Should().NotBeNull();
        result.Checks!.Expiry.Should().BeFalse();
        result.ErrorMessage.Should().Contain("expired");
    }

    [Fact]
    public async Task HandleAsync_RevokedCredential_ReturnsInvalidResult()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();

        var credentialResult = Credential.Create(
            walletId,
            issuerId,
            "DriversLicense",
            "{\"type\":\"DriversLicense\"}",
            "schema:driverslicense:1.0");

        var credential = credentialResult.Value;
        credential.SetTenantId("test-tenant");
        credential.Activate();
        credential.Revoke("Security breach");

        var command = new VerifyCredentialCommand
        {
            CredentialId = credential.Id.ToString()
        };

        _cacheServiceMock.Setup(x => x.GetAsync<VerificationResultDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VerificationResultDto?)null);
        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Checks.Should().NotBeNull();
        result.Checks!.Revocation.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Revoked");
    }

    [Fact]
    public async Task HandleAsync_CachedResult_ReturnsCachedValue()
    {
        // Arrange
        var command = new VerifyCredentialCommand
        {
            CredentialId = Guid.NewGuid().ToString()
        };

        var cachedResult = new VerificationResultDto
        {
            IsValid = true,
            VerifiedAt = DateTime.UtcNow.AddMinutes(-2),
            Checks = new VerificationChecksDto
            {
                Signature = true,
                Expiry = true,
                Issuer = true,
                Schema = true,
                Revocation = true
            }
        };

        _cacheServiceMock.Setup(x => x.GetAsync<VerificationResultDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(cachedResult);
        result.IsValid.Should().BeTrue();
        _credentialRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_CommandWithJwtData_UsesProvidedJwt()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var storedData = "{\"type\":\"DriversLicense\"}";
        var providedJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";

        var credentialResult = Credential.Create(
            walletId,
            issuerId,
            "DriversLicense",
            storedData,
            "schema:driverslicense:1.0");

        var credential = credentialResult.Value;
        credential.SetTenantId("test-tenant");
        credential.Activate();

        var command = new VerifyCredentialCommand
        {
            CredentialId = credential.Id.ToString(),
            CredentialData = providedJwt // Provide JWT in command
        };

        _cacheServiceMock.Setup(x => x.GetAsync<VerificationResultDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VerificationResultDto?)null);
        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _jwtSigningServiceMock.Setup(x => x.VerifyCredentialAsync(providedJwt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _cacheServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<VerificationResultDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Checks!.Signature.Should().BeTrue();
        _jwtSigningServiceMock.Verify(x => x.VerifyCredentialAsync(providedJwt, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_BiometricRequired_WithValidToken_PassesVerification()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var credentialResult = Credential.Create(
            walletId,
            issuerId,
            "DriversLicense",
            "{\"type\":\"DriversLicense\"}",
            "schema:driverslicense:1.0");

        var credential = credentialResult.Value;
        credential.SetTenantId("test-tenant");
        credential.Activate();

        var walletResult = Wallet.Create(personId, "Test Wallet");
        var wallet = walletResult.Value;
        wallet.SetTenantId("test-tenant");

        var person = Person.Create("John", "Doe", "john@example.com", "+61400000000").Value;
        person.SetTenantId("test-tenant");
        person.MarkAsVerified();

        var command = new VerifyCredentialCommand
        {
            CredentialId = credential.Id.ToString(),
            VerificationOptions = new Dictionary<string, object>
            {
                ["requireBiometric"] = "true",
                ["biometricToken"] = "valid_biometric_token_abc123def456",
                ["biometricTimestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            }
        };

        _cacheServiceMock.Setup(x => x.GetAsync<VerificationResultDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VerificationResultDto?)null);
        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _walletRepositoryMock.Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        _personRepositoryMock.Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);
        _cacheServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<VerificationResultDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        _walletRepositoryMock.Verify(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()), Times.Once);
        _personRepositoryMock.Verify(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_BiometricRequired_WithoutToken_FailsVerification()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();

        var credentialResult = Credential.Create(
            walletId,
            issuerId,
            "DriversLicense",
            "{\"type\":\"DriversLicense\"}",
            "schema:driverslicense:1.0");

        var credential = credentialResult.Value;
        credential.SetTenantId("test-tenant");
        credential.Activate();

        var command = new VerifyCredentialCommand
        {
            CredentialId = credential.Id.ToString(),
            VerificationOptions = new Dictionary<string, object>
            {
                ["requireBiometric"] = "true"
                // No biometricToken provided
            }
        };

        _cacheServiceMock.Setup(x => x.GetAsync<VerificationResultDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VerificationResultDto?)null);
        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Biometric verification");
    }

    [Fact]
    public async Task HandleAsync_BiometricRequired_ExpiredToken_FailsVerification()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var credentialResult = Credential.Create(
            walletId,
            issuerId,
            "DriversLicense",
            "{\"type\":\"DriversLicense\"}",
            "schema:driverslicense:1.0");

        var credential = credentialResult.Value;
        credential.SetTenantId("test-tenant");
        credential.Activate();

        var walletResult = Wallet.Create(personId, "Test Wallet");
        var wallet = walletResult.Value;
        wallet.SetTenantId("test-tenant");

        var person = Person.Create("John", "Doe", "john@example.com", "+61400000000").Value;
        person.SetTenantId("test-tenant");
        person.MarkAsVerified();

        var command = new VerifyCredentialCommand
        {
            CredentialId = credential.Id.ToString(),
            VerificationOptions = new Dictionary<string, object>
            {
                ["requireBiometric"] = "true",
                ["biometricToken"] = "valid_biometric_token_abc123def456",
                ["biometricTimestamp"] = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds() // 5 minutes old
            }
        };

        _cacheServiceMock.Setup(x => x.GetAsync<VerificationResultDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VerificationResultDto?)null);
        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _walletRepositoryMock.Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        _personRepositoryMock.Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Biometric verification");
    }
}