using System;
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
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using Xunit;

namespace NumbatWallet.Application.Tests.Commands.Credentials;

public class ShareCredentialCommandHandlerTests
{
    private readonly Mock<ICredentialRepository> _credentialRepositoryMock;
    private readonly Mock<ICredentialSharingService> _sharingServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<ShareCredentialCommandHandler>> _loggerMock;
    private readonly ShareCredentialCommandHandler _handler;

    public ShareCredentialCommandHandlerTests()
    {
        _credentialRepositoryMock = new Mock<ICredentialRepository>();
        _sharingServiceMock = new Mock<ICredentialSharingService>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<ShareCredentialCommandHandler>>();
        _handler = new ShareCredentialCommandHandler(
            _credentialRepositoryMock.Object,
            _sharingServiceMock.Object,
            _emailServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCredential_ReturnsShareResult()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var recipientEmail = "recipient@example.com";
        var shareUrl = "https://wallet.wa.gov.au/share/abc123";
        // var shareCode = "ABC123DEF456"; // Not used in test
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        var credentialResult = Credential.Create(
            walletId,
            issuerId,
            "DriversLicense",
            "{}",
            "schema:driverslicense:1.0");

        credentialResult.IsSuccess.Should().BeTrue();
        var credential = credentialResult.Value;
        credential.Activate();

        var command = new ShareCredentialCommand(
            credentialId,
            recipientEmail,
            60,
            true,
            "1234");

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        _sharingServiceMock.Setup(x => x.CreateShareLinkAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(shareUrl);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.ShareUrl.Should().Be(shareUrl);
        result.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(5));

        _emailServiceMock.Verify(x => x.SendEmailAsync(
            recipientEmail,
            "Credential Shared With You",
            It.IsAny<string>(),
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_CredentialNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new ShareCredentialCommand(
            Guid.NewGuid(),
            "recipient@example.com",
            60,
            false,
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
            "DriversLicense",
            "{}",
            "schema:driverslicense:1.0");

        var credential = credentialResult.Value;
        // Not activated - status is Pending

        var command = new ShareCredentialCommand(
            credentialId,
            "recipient@example.com",
            60,
            false,
            null);

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.HandleAsync(command));
        exception.Message.Should().Contain("Cannot share inactive credential");
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
            "DriversLicense",
            "{}",
            "schema:driverslicense:1.0");

        var credential = credentialResult.Value;
        credential.Activate();
        credential.SetExpiry(DateTimeOffset.UtcNow.AddDays(-1)); // Expired yesterday

        var command = new ShareCredentialCommand(
            credentialId,
            "recipient@example.com",
            60,
            false,
            null);

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.HandleAsync(command));
        exception.Message.Should().Contain("Cannot share inactive credential");
    }

    [Fact]
    public async Task HandleAsync_WithoutPin_SendsEmailWithoutPinNote()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var recipientEmail = "recipient@example.com";
        var shareUrl = "https://wallet.wa.gov.au/share/xyz789";

        var credentialResult = Credential.Create(
            walletId,
            issuerId,
            "Passport",
            "{}",
            "schema:passport:1.0");

        var credential = credentialResult.Value;
        credential.Activate();

        var command = new ShareCredentialCommand(
            credentialId,
            recipientEmail,
            30,
            false,
            null);

        _credentialRepositoryMock.Setup(x => x.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        _sharingServiceMock.Setup(x => x.CreateShareLinkAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(shareUrl);

        string capturedEmailBody = null!;
        _emailServiceMock.Setup(x => x.SendEmailAsync(
                recipientEmail,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string, bool, CancellationToken>((to, subject, body, isHtml, ct) =>
            {
                capturedEmailBody = body;
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        capturedEmailBody.Should().NotContain("You will need a PIN");
        capturedEmailBody.Should().Contain(shareUrl);
        capturedEmailBody.Should().Contain("Passport");
    }
}