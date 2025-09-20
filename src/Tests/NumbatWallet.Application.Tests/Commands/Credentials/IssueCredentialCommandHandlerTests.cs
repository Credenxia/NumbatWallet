using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.Commands.Credentials.Handlers;
using NumbatWallet.Application.Exceptions;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Enums;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Tests.Commands.Credentials;

public class IssueCredentialCommandHandlerTests
{
    private readonly Mock<ICredentialRepository> _credentialRepositoryMock;
    private readonly Mock<IWalletRepository> _walletRepositoryMock;
    private readonly Mock<IIssuerRepository> _issuerRepositoryMock;
    private readonly Mock<IEventDispatcher> _eventDispatcherMock;
    private readonly Mock<ILogger<IssueCredentialCommandHandler>> _loggerMock;
    private readonly IssueCredentialCommandHandler _handler;

    public IssueCredentialCommandHandlerTests()
    {
        _credentialRepositoryMock = new Mock<ICredentialRepository>();
        _walletRepositoryMock = new Mock<IWalletRepository>();
        _issuerRepositoryMock = new Mock<IIssuerRepository>();
        _eventDispatcherMock = new Mock<IEventDispatcher>();
        _loggerMock = new Mock<ILogger<IssueCredentialCommandHandler>>();

        _handler = new IssueCredentialCommandHandler(
            _credentialRepositoryMock.Object,
            _walletRepositoryMock.Object,
            _issuerRepositoryMock.Object,
            _eventDispatcherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_IssuesCredentialSuccessfully()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var command = new IssueCredentialCommand(
            walletId,
            CredentialType.DriversLicense,
            "John Doe",
            new Dictionary<string, object>
            {
                ["licenseNumber"] = "DL123456",
                ["fullName"] = "John Doe",
                ["expiryDate"] = DateTime.UtcNow.AddYears(5).ToString("yyyy-MM-dd"),
                ["state"] = "WA"
            },
            DateTime.UtcNow,
            DateTime.UtcNow.AddYears(5),
            issuerId.ToString(),
            organizationId);

        var walletResult = Wallet.Create(personId, "Test Wallet");
        var wallet = walletResult.Value;
        wallet.SetTenantId("test-tenant");
        // Wallet is active by default after creation

        var issuerResult = Issuer.Create("Test Issuer", "TST", "test.issuer.com");
        var issuer = issuerResult.Value;

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _issuerRepositoryMock
            .Setup(x => x.GetByIdAsync(issuerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(issuer);

        _credentialRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Credential c, CancellationToken ct) => c);

        _eventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<CredentialIssuedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be("DriversLicense");
        result.HolderId.Should().Be(walletId.ToString());
        result.IssuerId.Should().Be(issuer.Id.ToString());
        result.Status.Should().Be(CredentialStatus.Active.ToString());
        result.IsRevoked.Should().BeFalse();
        result.CredentialSubject.Should().ContainKey("licenseNumber");
        result.ExpirationDate.Should().NotBeNull();

        _credentialRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Credential>(c =>
                c.WalletId == walletId &&
                c.IssuerId == issuer.Id &&
                c.CredentialType == "DriversLicense" &&
                c.Status == CredentialStatus.Active),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _eventDispatcherMock.Verify(
            x => x.DispatchAsync(It.Is<CredentialIssuedEvent>(e =>
                e.WalletId == walletId &&
                e.IssuerId == issuer.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenWalletNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new IssueCredentialCommand(
            Guid.NewGuid(),
            CredentialType.Passport,
            "Jane Doe",
            new Dictionary<string, object>
            {
                ["passportNumber"] = "P123456789",
                ["fullName"] = "Jane Doe"
            },
            DateTime.UtcNow,
            DateTime.UtcNow.AddYears(10),
            Guid.NewGuid().ToString(),
            Guid.NewGuid());

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.HandleAsync(command, CancellationToken.None));

        _credentialRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenIssuerNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();

        var command = new IssueCredentialCommand(
            walletId,
            CredentialType.StudentId,
            "John Smith",
            new Dictionary<string, object>
            {
                ["idNumber"] = "STU123456789",
                ["fullName"] = "John Smith"
            },
            DateTime.UtcNow,
            DateTime.UtcNow.AddYears(4),
            issuerId.ToString(),
            Guid.NewGuid());

        var walletResult = Wallet.Create(personId, "Test Wallet");
        var wallet = walletResult.Value;
        wallet.SetTenantId("test-tenant");
        // Wallet is active by default after creation

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _issuerRepositoryMock
            .Setup(x => x.GetByIdAsync(issuerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Issuer?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.HandleAsync(command, CancellationToken.None));

        _credentialRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenWalletIsNotActive_ThrowsBusinessRuleException()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var command = new IssueCredentialCommand(
            walletId,
            CredentialType.StudentId,
            "Student Name",
            new Dictionary<string, object>
            {
                ["studentId"] = "STU123456",
                ["fullName"] = "Student Name"
            },
            DateTime.UtcNow,
            DateTime.UtcNow.AddYears(1),
            issuerId.ToString(),
            organizationId);

        var walletResult = Wallet.Create(personId, "Test Wallet");
        var wallet = walletResult.Value;
        // Suspend wallet to make it inactive
        wallet.Suspend("Test suspension");

        var issuerResult = Issuer.Create("Test Issuer", "TST", "test.issuer.com");
        var issuer = issuerResult.Value;

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _issuerRepositoryMock
            .Setup(x => x.GetByIdAsync(issuerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(issuer);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _handler.HandleAsync(command, CancellationToken.None));

        _credentialRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithoutExpirationDate_IssuesCredentialWithoutExpiry()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var command = new IssueCredentialCommand(
            walletId,
            CredentialType.MembershipCard,
            "John Doe",
            new Dictionary<string, object>
            {
                ["memberId"] = "MEM123456",
                ["fullName"] = "John Doe",
                ["memberSince"] = DateTime.UtcNow.ToString("yyyy-MM-dd")
            },
            DateTime.UtcNow,
            null, // No expiration
            issuerId.ToString(),
            organizationId);

        var walletResult = Wallet.Create(personId, "Test Wallet");
        var wallet = walletResult.Value;
        wallet.SetTenantId("test-tenant");
        // Wallet is active by default after creation

        var issuerResult = Issuer.Create("Test Issuer", "TST", "test.issuer.com");
        var issuer = issuerResult.Value;

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _issuerRepositoryMock
            .Setup(x => x.GetByIdAsync(issuerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(issuer);

        _credentialRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Credential c, CancellationToken ct) => c);

        _eventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<CredentialIssuedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ExpirationDate.Should().BeNull();
        result.Status.Should().Be(CredentialStatus.Active.ToString());

        _credentialRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Credential>(c => c.ExpiresAt == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}