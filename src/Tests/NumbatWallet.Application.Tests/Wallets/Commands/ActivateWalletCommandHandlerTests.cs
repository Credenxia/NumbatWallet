using NumbatWallet.Application.Commands.Wallets;
using NumbatWallet.Application.Commands.Wallets.Handlers;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Domain.Aggregates;
using Microsoft.Extensions.Logging;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Exceptions;
using EntityNotFoundException = NumbatWallet.Application.Common.Exceptions.EntityNotFoundException;

namespace NumbatWallet.Application.Tests.Wallets.Commands;

public class ActivateWalletCommandHandlerTests
{
    private readonly Mock<IWalletRepository> _walletRepositoryMock;
    private readonly Mock<IPersonRepository> _personRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentTenantService> _tenantServiceMock;
    private readonly Mock<ILogger<ActivateWalletCommandHandler>> _loggerMock;
    private readonly ActivateWalletCommandHandler _handler;
    private const string DefaultTenantId = "test-tenant";

    public ActivateWalletCommandHandlerTests()
    {
        _walletRepositoryMock = new Mock<IWalletRepository>();
        _personRepositoryMock = new Mock<IPersonRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _tenantServiceMock = new Mock<ICurrentTenantService>();
        _loggerMock = new Mock<ILogger<ActivateWalletCommandHandler>>();

        _tenantServiceMock.Setup(x => x.TenantId).Returns(DefaultTenantId);

        _handler = new ActivateWalletCommandHandler(
            _walletRepositoryMock.Object,
            _personRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _tenantServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReactivatesWalletSuccessfully()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var command = new ActivateWalletCommand(walletId, "1234"); // PIN provided for suspended wallet

        var walletResult = Wallet.Create(personId, "Test Wallet");
        var wallet = walletResult.Value;
        wallet.SetTenantId(DefaultTenantId);
        wallet.Suspend("Test suspension");

        var person = Person.Create("John", "Doe", "john@example.com", "+61400000000").Value;
        person.SetTenantId(DefaultTenantId);
        person.SetPin("1234"); // Set PIN for validation

        _walletRepositoryMock.Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        _personRepositoryMock.Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);
        _walletRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _personRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Person>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask); // Added: person repository needs update mock
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(wallet.Id.ToString());
        result.IsActive.Should().BeTrue();
        wallet.Status.Should().Be(WalletStatus.Active);

        _walletRepositoryMock.Verify(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()), Times.Once);
        _walletRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WalletNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var command = new ActivateWalletCommand(walletId);

        _walletRepositoryMock.Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"*Wallet*{walletId}*");

        _walletRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WalletFromDifferentTenant_ThrowsEntityNotFoundException()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var command = new ActivateWalletCommand(walletId);

        var walletResult = Wallet.Create(personId, "Test Wallet");
        var wallet = walletResult.Value;
        wallet.SetTenantId("different-tenant");

        _walletRepositoryMock.Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"*Wallet*{walletId}*");

        _walletRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SuspendedWalletWithoutPin_ThrowsBusinessRuleException()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var command = new ActivateWalletCommand(walletId); // No PIN provided

        var walletResult = Wallet.Create(personId, "Test Wallet");
        var wallet = walletResult.Value;
        wallet.SetTenantId(DefaultTenantId);
        wallet.Suspend("Test suspension");

        var person = Person.Create("John", "Doe", "john@example.com", "+61400000000").Value;
        person.SetTenantId(DefaultTenantId);

        _walletRepositoryMock.Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        _personRepositoryMock.Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*PIN is required*");

        _walletRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SuspendedWalletWithEmptyPin_ThrowsBusinessRuleException()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var command = new ActivateWalletCommand(walletId, "   "); // Empty/whitespace PIN

        var walletResult = Wallet.Create(personId, "Test Wallet");
        var wallet = walletResult.Value;
        wallet.SetTenantId(DefaultTenantId);
        wallet.Suspend("Test suspension");

        var person = Person.Create("John", "Doe", "john@example.com", "+61400000000").Value;
        person.SetTenantId(DefaultTenantId);

        _walletRepositoryMock.Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        _personRepositoryMock.Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*PIN is required*");

        _walletRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SuspendedWalletWithValidPin_ReactivatesSuccessfully()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var command = new ActivateWalletCommand(walletId, "1234"); // PIN provided

        var walletResult = Wallet.Create(personId, "Test Wallet");
        var wallet = walletResult.Value;
        wallet.SetTenantId(DefaultTenantId);
        wallet.Suspend("Test suspension");

        var person = Person.Create("John", "Doe", "john@example.com", "+61400000000").Value;
        person.SetTenantId(DefaultTenantId);
        person.SetPin("1234"); // Set PIN for validation

        _walletRepositoryMock.Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        _personRepositoryMock.Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);
        _walletRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _personRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Person>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask); // Added: person repository needs update mock
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeTrue();
        wallet.Status.Should().Be(WalletStatus.Active);

        _walletRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_LockedWalletWithPin_ThrowsBusinessRuleException()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var command = new ActivateWalletCommand(walletId, "1234"); // PIN provided

        var walletResult = Wallet.Create(personId, "Test Wallet");
        var wallet = walletResult.Value;
        wallet.SetTenantId(DefaultTenantId);
        wallet.Lock("Security lock");

        var person = Person.Create("John", "Doe", "john@example.com", "+61400000000").Value;
        person.SetTenantId(DefaultTenantId);
        person.SetPin("1234");

        _walletRepositoryMock.Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        _personRepositoryMock.Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Cannot reactivate a locked wallet*");

        _walletRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AlreadyActiveWallet_ThrowsBusinessRuleException()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var command = new ActivateWalletCommand(walletId);

        var walletResult = Wallet.Create(personId, "Test Wallet");
        var wallet = walletResult.Value;
        wallet.SetTenantId(DefaultTenantId);
        // Wallet is already active by default

        var person = Person.Create("John", "Doe", "john@example.com", "+61400000000").Value;
        person.SetTenantId(DefaultTenantId);

        _walletRepositoryMock.Setup(x => x.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        _personRepositoryMock.Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Wallet is already active*");

        _walletRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}