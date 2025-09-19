using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.Queries.Wallets;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Repositories;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Tests.Queries.Wallets;

public class GetWalletQueryTests
{
    private readonly Mock<IWalletRepository> _walletRepositoryMock;
    private readonly Mock<IPersonRepository> _personRepositoryMock;
    private readonly Mock<ICredentialRepository> _credentialRepositoryMock;
    private readonly Mock<ILogger<GetWalletQueryHandler>> _loggerMock;
    private readonly GetWalletQueryHandler _handler;

    public GetWalletQueryTests()
    {
        _walletRepositoryMock = new Mock<IWalletRepository>();
        _personRepositoryMock = new Mock<IPersonRepository>();
        _credentialRepositoryMock = new Mock<ICredentialRepository>();
        _loggerMock = new Mock<ILogger<GetWalletQueryHandler>>();

        _handler = new GetWalletQueryHandler(
            _walletRepositoryMock.Object,
            _personRepositoryMock.Object,
            _credentialRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_ReturnsWalletDto()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var wallet = Wallet.Create(personId, "Test Wallet").Value;
        var person = Person.Create("John", "Doe", "john@example.com", "+61412345678").Value;

        var query = new GetWalletQuery
        {
            WalletId = walletId.ToString(),
            IncludeCredentials = false
        };

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _personRepositoryMock
            .Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(wallet.Id.ToString());
        result.PersonId.Should().Be(personId.ToString());
        result.PersonName.Should().Be("John Doe");
        result.Name.Should().Be("Test Wallet");
        result.Status.Should().Be(WalletStatus.Active.ToString());
        result.IsActive.Should().BeTrue();
        result.IsSuspended.Should().BeFalse();
        result.CredentialCount.Should().Be(0);

        _credentialRepositoryMock.Verify(
            x => x.FindAsync(It.IsAny<Domain.Specifications.CredentialByWalletSpecification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithIncludeCredentials_ReturnsCredentialCount()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var wallet = Wallet.Create(personId, "Test Wallet").Value;
        var person = Person.Create("John", "Doe", "john@example.com", "+61412345678").Value;

        var credential1 = Credential.Create(walletId, issuerId, "DriverLicence", "{}", "schema1").Value;
        var credential2 = Credential.Create(walletId, issuerId, "ProofOfAge", "{}", "schema2").Value;

        var query = new GetWalletQuery
        {
            WalletId = walletId.ToString(),
            IncludeCredentials = true
        };

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _personRepositoryMock
            .Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        _credentialRepositoryMock
            .Setup(x => x.FindAsync(It.IsAny<Domain.Specifications.CredentialByWalletSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Credential> { credential1, credential2 });

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.CredentialCount.Should().Be(2);

        _credentialRepositoryMock.Verify(
            x => x.FindAsync(It.IsAny<Domain.Specifications.CredentialByWalletSpecification>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WalletNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var query = new GetWalletQuery
        {
            WalletId = walletId.ToString()
        };

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _handler.HandleAsync(query));

        _personRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_InvalidWalletId_ThrowsFormatException()
    {
        // Arrange
        var query = new GetWalletQuery
        {
            WalletId = "invalid-guid"
        };

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(
            () => _handler.HandleAsync(query));

        _walletRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_PersonNotFound_ReturnsUnknownPersonName()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var wallet = Wallet.Create(personId, "Test Wallet").Value;

        var query = new GetWalletQuery
        {
            WalletId = walletId.ToString()
        };

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _personRepositoryMock
            .Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Person?)null);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.PersonName.Should().Be("Unknown");
    }
}