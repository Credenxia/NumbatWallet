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

public class BulkIssueCredentialsCommandHandlerTests
{
    private readonly Mock<ICredentialRepository> _credentialRepositoryMock;
    private readonly Mock<IWalletRepository> _walletRepositoryMock;
    private readonly Mock<IIssuerRepository> _issuerRepositoryMock;
    private readonly Mock<IEventDispatcher> _eventDispatcherMock;
    private readonly Mock<ILogger<BulkIssueCredentialsCommandHandler>> _loggerMock;
    private readonly BulkIssueCredentialsCommandHandler _handler;

    public BulkIssueCredentialsCommandHandlerTests()
    {
        _credentialRepositoryMock = new Mock<ICredentialRepository>();
        _walletRepositoryMock = new Mock<IWalletRepository>();
        _issuerRepositoryMock = new Mock<IIssuerRepository>();
        _eventDispatcherMock = new Mock<IEventDispatcher>();
        _loggerMock = new Mock<ILogger<BulkIssueCredentialsCommandHandler>>();

        _handler = new BulkIssueCredentialsCommandHandler(
            _credentialRepositoryMock.Object,
            _walletRepositoryMock.Object,
            _issuerRepositoryMock.Object,
            _eventDispatcherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithAllValidWallets_IssuesCredentialsToAll()
    {
        // Arrange
        var walletIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var issuerId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var template = new Dictionary<string, object>
        {
            ["courseId"] = "CS101",
            ["courseName"] = "Introduction to Computer Science",
            ["institution"] = "Test University"
        };

        var command = new BulkIssueCredentialsCommand(
            walletIds,
            CredentialType.EducationalCertificate,
            template,
            issuerId.ToString(),
            organizationId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddYears(2));

        var issuerResult = Issuer.Create("Test University", "TST", "test.edu");
        var issuer = issuerResult.Value;

        _issuerRepositoryMock
            .Setup(x => x.GetByIdAsync(issuerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(issuer);

        // Setup wallets
        foreach (var walletId in walletIds)
        {
            var personId = Guid.NewGuid();
            var walletResult = Wallet.Create(personId, $"Test Wallet {walletId}");
            var wallet = walletResult.Value;
            wallet.SetTenantId("test-tenant");

            _walletRepositoryMock
                .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(wallet);
        }

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
        result.TotalRequested.Should().Be(3);
        result.SuccessCount.Should().Be(3);
        result.FailureCount.Should().Be(0);
        result.IssuedCredentialIds.Should().HaveCount(3);
        result.Errors.Should().BeEmpty();

        _credentialRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        _eventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<CredentialIssuedEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task HandleAsync_WithSomeInvalidWallets_IssuesCredentialsToValidOnes()
    {
        // Arrange
        var validWalletId = Guid.NewGuid();
        var inactiveWalletId = Guid.NewGuid();
        var notFoundWalletId = Guid.NewGuid();
        var walletIds = new List<Guid> { validWalletId, inactiveWalletId, notFoundWalletId };
        var issuerId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var template = new Dictionary<string, object>
        {
            ["certificateType"] = "Training",
            ["completionDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd")
        };

        var command = new BulkIssueCredentialsCommand(
            walletIds,
            CredentialType.ProfessionalLicense,
            template,
            issuerId.ToString(),
            organizationId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddYears(1));

        var issuerResult = Issuer.Create("Test Issuer", "TST", "test.com");
        var issuer = issuerResult.Value;

        _issuerRepositoryMock
            .Setup(x => x.GetByIdAsync(issuerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(issuer);

        // Setup valid wallet
        var validPersonId = Guid.NewGuid();
        var validWalletResult = Wallet.Create(validPersonId, "Valid Wallet");
        var validWallet = validWalletResult.Value;
        validWallet.SetTenantId("test-tenant");
        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(validWalletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validWallet);

        // Setup inactive wallet
        var inactivePersonId = Guid.NewGuid();
        var inactiveWalletResult = Wallet.Create(inactivePersonId, "Inactive Wallet");
        var inactiveWallet = inactiveWalletResult.Value;
        inactiveWallet.Suspend("Testing");
        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(inactiveWalletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveWallet);

        // Setup not found wallet
        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(notFoundWalletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

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
        result.TotalRequested.Should().Be(3);
        result.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(2);
        result.IssuedCredentialIds.Should().HaveCount(1);
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.WalletId == inactiveWalletId);
        result.Errors.Should().Contain(e => e.WalletId == notFoundWalletId);

        _credentialRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _eventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<CredentialIssuedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenIssuerNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var walletIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var issuerId = Guid.NewGuid();
        var command = new BulkIssueCredentialsCommand(
            walletIds,
            CredentialType.MembershipCard,
            new Dictionary<string, object>(),
            issuerId.ToString(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            null);

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
    public async Task HandleAsync_WithEmptyWalletList_ReturnsEmptyResult()
    {
        // Arrange
        var issuerId = Guid.NewGuid();
        var command = new BulkIssueCredentialsCommand(
            new List<Guid>(),
            CredentialType.HealthCard,
            new Dictionary<string, object>(),
            issuerId.ToString(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            null);

        var issuerResult = Issuer.Create("Test Issuer", "TST", "test.com");
        var issuer = issuerResult.Value;

        _issuerRepositoryMock
            .Setup(x => x.GetByIdAsync(issuerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(issuer);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalRequested.Should().Be(0);
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(0);
        result.IssuedCredentialIds.Should().BeEmpty();
        result.Errors.Should().BeEmpty();

        _credentialRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}