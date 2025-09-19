using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NumbatWallet.Application.Commands.Wallets;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Repositories;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;
using Xunit;

namespace NumbatWallet.Application.Tests.Commands.Wallets;

public class SuspendWalletCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IWalletRepository> _walletRepositoryMock;
    private readonly Mock<ILogger<SuspendWalletCommandHandler>> _loggerMock;
    private readonly SuspendWalletCommandHandler _handler;

    public SuspendWalletCommandTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _walletRepositoryMock = new Mock<IWalletRepository>();
        _loggerMock = new Mock<ILogger<SuspendWalletCommandHandler>>();

        _handler = new SuspendWalletCommandHandler(
            _unitOfWorkMock.Object,
            _walletRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_SuspendsWallet()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var wallet = Wallet.Create(personId, "Test Wallet").Value;

        var command = new SuspendWalletCommand
        {
            WalletId = walletId.ToString(),
            Reason = "Security violation",
            SuspendedUntil = DateTimeOffset.UtcNow.AddDays(30)
        };

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _walletRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().BeTrue();

        _walletRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WalletNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var command = new SuspendWalletCommand
        {
            WalletId = walletId.ToString(),
            Reason = "Security violation"
        };

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _handler.HandleAsync(command));

        _walletRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WalletAlreadySuspended_ReturnsFalse()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var wallet = Wallet.Create(personId, "Test Wallet").Value;
        wallet.Suspend("Previous suspension");

        var command = new SuspendWalletCommand
        {
            WalletId = walletId.ToString(),
            Reason = "Another reason"
        };

        _walletRepositoryMock
            .Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().BeFalse();

        _walletRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_InvalidWalletId_ThrowsFormatException()
    {
        // Arrange
        var command = new SuspendWalletCommand
        {
            WalletId = "invalid-guid",
            Reason = "Test reason"
        };

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(
            () => _handler.HandleAsync(command));

        _walletRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}