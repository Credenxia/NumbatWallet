using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NumbatWallet.Application.Commands.Wallets;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Repositories;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Tests.Commands.Wallets;

public class CreateWalletCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IWalletRepository> _walletRepositoryMock;
    private readonly Mock<IPersonRepository> _personRepositoryMock;
    private readonly Mock<ILogger<CreateWalletCommandHandler>> _loggerMock;
    private readonly CreateWalletCommandHandler _handler;

    public CreateWalletCommandTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _walletRepositoryMock = new Mock<IWalletRepository>();
        _personRepositoryMock = new Mock<IPersonRepository>();
        _loggerMock = new Mock<ILogger<CreateWalletCommandHandler>>();

        _handler = new CreateWalletCommandHandler(
            _unitOfWorkMock.Object,
            _walletRepositoryMock.Object,
            _personRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesWallet()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var person = Person.Create("John", "Doe", "john@example.com", "+61412345678").Value;
        var command = new CreateWalletCommand
        {
            PersonId = personId.ToString(),
            Name = "My Wallet",
            Metadata = new Dictionary<string, string> { ["key"] = "value" }
        };

        _personRepositoryMock
            .Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        _walletRepositoryMock
            .Setup(x => x.FindAsync(It.IsAny<Domain.Specifications.WalletByPersonSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Wallet>());

        _walletRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet w, CancellationToken ct) => w);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.PersonId.Should().Be(personId.ToString());
        result.Name.Should().Be("My Wallet");
        result.IsActive.Should().BeTrue();
        result.IsSuspended.Should().BeFalse();

        _walletRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PersonNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var command = new CreateWalletCommand
        {
            PersonId = personId.ToString(),
            Name = "My Wallet"
        };

        _personRepositoryMock
            .Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Person?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _handler.HandleAsync(command));

        _walletRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_PersonAlreadyHasActiveWallet_ThrowsDomainValidationException()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var person = Person.Create("John", "Doe", "john@example.com", "+61412345678").Value;
        var existingWallet = Wallet.Create(personId, "Existing Wallet").Value;

        var command = new CreateWalletCommand
        {
            PersonId = personId.ToString(),
            Name = "My Wallet"
        };

        _personRepositoryMock
            .Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        _walletRepositoryMock
            .Setup(x => x.FindAsync(It.IsAny<Domain.Specifications.WalletByPersonSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Wallet> { existingWallet });

        // Act & Assert
        await Assert.ThrowsAsync<DomainValidationException>(
            () => _handler.HandleAsync(command));

        _walletRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_InvalidPersonId_ThrowsFormatException()
    {
        // Arrange
        var command = new CreateWalletCommand
        {
            PersonId = "invalid-guid",
            Name = "My Wallet"
        };

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(
            () => _handler.HandleAsync(command));

        _personRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _walletRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}