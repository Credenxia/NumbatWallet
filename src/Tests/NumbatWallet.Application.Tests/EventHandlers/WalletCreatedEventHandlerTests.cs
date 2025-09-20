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

public class WalletCreatedEventHandlerTests
{
    private readonly Mock<ILogger<WalletCreatedEventHandler>> _loggerMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IWalletRepository> _walletRepositoryMock;
    private readonly WalletCreatedEventHandler _handler;

    public WalletCreatedEventHandlerTests()
    {
        _loggerMock = new Mock<ILogger<WalletCreatedEventHandler>>();
        _notificationServiceMock = new Mock<INotificationService>();
        _auditServiceMock = new Mock<IAuditService>();
        _walletRepositoryMock = new Mock<IWalletRepository>();

        _handler = new WalletCreatedEventHandler(
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
        var tenantId = Guid.NewGuid().ToString();
        var walletDid = "did:wallet:123456";
        var createdAt = DateTimeOffset.UtcNow;

        var walletResult = Wallet.Create(personId, "Test Wallet");
        walletResult.IsSuccess.Should().BeTrue();
        var wallet = walletResult.Value;
        wallet.SetTenantId(tenantId);

        var domainEvent = new WalletCreatedEvent(
            walletId,
            personId,
            tenantId,
            walletDid,
            createdAt);

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        // Act
        await _handler.HandleAsync(domainEvent);

        // Assert
        _notificationServiceMock.Verify(
            x => x.SendNotificationAsync(
                personId,
                "Wallet Created",
                It.Is<string>(msg => msg.Contains("digital wallet has been created successfully")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _auditServiceMock.Verify(
            x => x.LogAccessAsync(
                It.Is<object>(entry => VerifyAuditEntry(entry, walletId, personId, tenantId)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var tenantId = Guid.NewGuid().ToString();
        var walletDid = "did:wallet:123456";
        var createdAt = DateTimeOffset.UtcNow;

        var walletResult = Wallet.Create(personId, "Test Wallet");
        walletResult.IsSuccess.Should().BeTrue();
        var wallet = walletResult.Value;
        wallet.SetTenantId(tenantId);

        var domainEvent = new WalletCreatedEvent(
            walletId,
            personId,
            tenantId,
            walletDid,
            createdAt);

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var cancellationToken = new CancellationToken();

        // Act
        await _handler.HandleAsync(domainEvent, cancellationToken);

        // Assert
        _notificationServiceMock.Verify(
            x => x.SendNotificationAsync(
                personId,
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

    [Fact]
    public async Task HandleAsync_LogsInformationMessages()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var tenantId = Guid.NewGuid().ToString();
        var walletDid = "did:wallet:123456";
        var createdAt = DateTimeOffset.UtcNow;

        var walletResult = Wallet.Create(personId, "Test Wallet");
        walletResult.IsSuccess.Should().BeTrue();
        var wallet = walletResult.Value;
        wallet.SetTenantId(tenantId);

        var domainEvent = new WalletCreatedEvent(
            walletId,
            personId,
            tenantId,
            walletDid,
            createdAt);

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        // Act
        await _handler.HandleAsync(domainEvent);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Handling WalletCreatedEvent for Wallet {walletId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"WalletCreatedEvent handled successfully for Wallet {walletId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private bool VerifyAuditEntry(object entry, Guid walletId, Guid personId, string tenantId)
    {
        if (entry is not AuditLogEntry auditEntry)
        {
            return false;
        }

        auditEntry.EntityType.Should().Be("Wallet");
        auditEntry.EntityId.Should().Be(walletId.ToString());
        auditEntry.Action.Should().Be("Created");
        auditEntry.UserId.Should().Be("System");
        auditEntry.TenantId.Should().Be(Guid.Parse(tenantId));
        auditEntry.MaxClassification.Should().Be(DataClassification.Protected);

        var changedFields = auditEntry.ChangedFields;
        changedFields.Should().ContainKey("WalletId");
        changedFields.Should().ContainKey("PersonId");
        changedFields.Should().ContainKey("WalletDid");
        changedFields.Should().ContainKey("CreatedAt");

        changedFields["WalletId"].Should().Be(walletId);
        changedFields["PersonId"].Should().Be(personId);

        return true;
    }
}