using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NumbatWallet.Application.EventHandlers;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Tests.EventHandlers;

public class CredentialIssuedEventHandlerTests
{
    private readonly Mock<ILogger<CredentialIssuedEventHandler>> _loggerMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IWalletRepository> _walletRepositoryMock;
    private readonly CredentialIssuedEventHandler _handler;

    public CredentialIssuedEventHandlerTests()
    {
        _loggerMock = new Mock<ILogger<CredentialIssuedEventHandler>>();
        _notificationServiceMock = new Mock<INotificationService>();
        _auditServiceMock = new Mock<IAuditService>();
        _walletRepositoryMock = new Mock<IWalletRepository>();

        _handler = new CredentialIssuedEventHandler(
            _loggerMock.Object,
            _notificationServiceMock.Object,
            _auditServiceMock.Object,
            _walletRepositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidEvent_SendsNotificationAndLogsAudit()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var credentialId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var credentialDid = "did:example:123456";

        var walletResult = Wallet.Create(personId, "Test Wallet");
        walletResult.IsSuccess.Should().BeTrue();
        var wallet = walletResult.Value;
        wallet.SetTenantId(Guid.NewGuid().ToString());

        var domainEvent = new CredentialIssuedEvent(
            credentialId,
            walletId,
            issuerId,
            "DriverLicense",
            credentialDid,
            DateTimeOffset.UtcNow);

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        // Act
        await _handler.HandleAsync(domainEvent);

        // Assert
        _notificationServiceMock.Verify(
            x => x.SendNotificationAsync(
                personId,
                "New Credential Issued",
                It.Is<string>(msg => msg.Contains("DriverLicense")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _auditServiceMock.Verify(
            x => x.LogAccessAsync(
                It.Is<object>(entry => VerifyAuditEntry(entry, credentialId, walletId)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenWalletNotFound_DoesNotSendNotification()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var credentialId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var credentialDid = "did:example:123456";

        var domainEvent = new CredentialIssuedEvent(
            credentialId,
            walletId,
            issuerId,
            "Passport",
            credentialDid,
            DateTimeOffset.UtcNow);

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        // Act
        await _handler.HandleAsync(domainEvent);

        // Assert
        _notificationServiceMock.Verify(
            x => x.SendNotificationAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _auditServiceMock.Verify(
            x => x.LogAccessAsync(
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var credentialId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var credentialDid = "did:example:123456";

        var walletResult = Wallet.Create(personId, "Test Wallet");
        walletResult.IsSuccess.Should().BeTrue();
        var wallet = walletResult.Value;
        wallet.SetTenantId(Guid.NewGuid().ToString());

        var domainEvent = new CredentialIssuedEvent(
            credentialId,
            walletId,
            issuerId,
            "NationalId",
            credentialDid,
            DateTimeOffset.UtcNow);

        var cancellationToken = new CancellationToken();

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, cancellationToken))
            .ReturnsAsync(wallet);

        // Act
        await _handler.HandleAsync(domainEvent, cancellationToken);

        // Assert
        _walletRepositoryMock.Verify(
            x => x.GetByIdAsync(walletId, cancellationToken),
            Times.Once);

        _notificationServiceMock.Verify(
            x => x.SendNotificationAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                cancellationToken),
            Times.Once);

        _auditServiceMock.Verify(
            x => x.LogAccessAsync(
                It.IsAny<object>(),
                cancellationToken),
            Times.Once);
    }

    private bool VerifyAuditEntry(object entry, Guid credentialId, Guid walletId)
    {
        if (entry is not AuditLogEntry auditEntry)
        {
            return false;
        }

        auditEntry.EntityType.Should().Be("Credential");
        auditEntry.EntityId.Should().Be(credentialId.ToString());
        auditEntry.Action.Should().Be("Issued");
        auditEntry.UserId.Should().Be("System");
        auditEntry.MaxClassification.Should().Be(DataClassification.Protected);

        var changedFields = auditEntry.ChangedFields;
        changedFields.Should().ContainKey("CredentialId");
        changedFields.Should().ContainKey("WalletId");
        changedFields.Should().ContainKey("CredentialType");
        changedFields.Should().ContainKey("IssuerId");
        changedFields.Should().ContainKey("CredentialDid");
        changedFields.Should().ContainKey("IssuedAt");

        changedFields["CredentialId"].Should().Be(credentialId);
        changedFields["WalletId"].Should().Be(walletId);

        return true;
    }
}