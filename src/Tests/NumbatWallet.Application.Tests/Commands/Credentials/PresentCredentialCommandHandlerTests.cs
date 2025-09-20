using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.Commands.Credentials.Handlers;
using NumbatWallet.Application.Exceptions;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using Xunit;

namespace NumbatWallet.Application.Tests.Commands.Credentials;

public class PresentCredentialCommandHandlerTests
{
    private readonly Mock<ICredentialRepository> _credentialRepositoryMock;
    private readonly Mock<IVerificationService> _verificationServiceMock;
    private readonly Mock<IEventDispatcher> _eventDispatcherMock;
    private readonly Mock<ILogger<PresentCredentialCommandHandler>> _loggerMock;
    private readonly PresentCredentialCommandHandler _handler;

    public PresentCredentialCommandHandlerTests()
    {
        _credentialRepositoryMock = new Mock<ICredentialRepository>();
        _verificationServiceMock = new Mock<IVerificationService>();
        _eventDispatcherMock = new Mock<IEventDispatcher>();
        _loggerMock = new Mock<ILogger<PresentCredentialCommandHandler>>();
        _handler = new PresentCredentialCommandHandler(
            _credentialRepositoryMock.Object,
            _verificationServiceMock.Object,
            _eventDispatcherMock.Object,
            _loggerMock.Object);
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

        _verificationServiceMock.Setup(x => x.CreatePresentationTokenAsync(
                credentialId,
                verifierId,
                purpose,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(presentationToken);

        _verificationServiceMock.Setup(x => x.CreateVerificationUrlAsync(
                presentationToken,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(verificationUrl);

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
    public async Task HandleAsync_NoSelectiveDisclosure_DiscloseAllClaims()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();

        var credentialData = new Dictionary<string, object>
        {
            { "fullName", "Jane Smith" },
            { "employeeId", "EMP001" },
            { "department", "IT" },
            { "startDate", "2020-01-15" }
        };

        var credentialResult = Credential.Create(
            walletId,
            issuerId,
            "EmployeeId",
            JsonSerializer.Serialize(credentialData),
            "schema:employeeid:1.0");

        var credential = credentialResult.Value;
        credential.Activate();

        var command = new PresentCredentialCommand(
            credentialId,
            "verifier_456",
            "Employment Verification",
            null); // No selective disclosure

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        _verificationServiceMock.Setup(x => x.CreatePresentationTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("token");

        _verificationServiceMock.Setup(x => x.CreateVerificationUrlAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("url");

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
    }

    [Fact]
    public async Task HandleAsync_EmitsPresentedEvent_WithCorrectData()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var verifierId = "verifier_test";
        var purpose = "Test Purpose";

        var credentialResult = Credential.Create(
            walletId,
            issuerId,
            "Custom",
            "{}",
            "schema:custom:1.0");

        var credential = credentialResult.Value;
        credential.Activate();

        var command = new PresentCredentialCommand(
            credentialId,
            verifierId,
            purpose,
            null);

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        _verificationServiceMock.Setup(x => x.CreatePresentationTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("token");

        _verificationServiceMock.Setup(x => x.CreateVerificationUrlAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("url");

        CredentialPresentedEvent? capturedEvent = null;
        _eventDispatcherMock.Setup(x => x.DispatchAsync(
                It.IsAny<CredentialPresentedEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<CredentialPresentedEvent, CancellationToken>((evt, ct) =>
            {
                capturedEvent = evt;
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
}