using Moq;
using FluentAssertions;
using NumbatWallet.Application.Wallets.Commands.CreateWallet;
using NumbatWallet.Application.Wallets.DTOs;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Domain.Aggregates;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Tests.Wallets.Commands;

public class CreateWalletCommandHandlerTests
{
    private readonly Mock<IWalletRepository> _walletRepositoryMock;
    private readonly Mock<IPersonRepository> _personRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<CreateWalletCommandHandler>> _loggerMock;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly CreateWalletCommandHandler _handler;

    public CreateWalletCommandHandlerTests()
    {
        _walletRepositoryMock = new Mock<IWalletRepository>();
        _personRepositoryMock = new Mock<IPersonRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<CreateWalletCommandHandler>>();
        _tenantServiceMock = new Mock<ITenantService>();

        _handler = new CreateWalletCommandHandler(
            _walletRepositoryMock.Object,
            _personRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _tenantServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesWalletSuccessfully()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateWalletCommand
        {
            PersonId = personId,
            Name = "My Digital Wallet",
            Description = "Primary wallet for credentials"
        };

        var personResult = Person.Create(
            "John",
            "Doe",
            "john.doe@example.com",
            "+61412345678");

        var person = personResult.Value;
        person.SetTenantId(tenantId.ToString());

        var expectedWalletResult = Wallet.Create(
            personId,
            command.Name);

        var expectedWallet = expectedWalletResult.Value;

        var walletDto = new WalletDto
        {
            Id = expectedWallet.Id,
            PersonId = personId,
            Name = command.Name,
            Description = command.Description,
            Status = "Active",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _tenantServiceMock.Setup(x => x.TenantId).Returns(tenantId);
        _personRepositoryMock.Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);
        _walletRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet w, CancellationToken c) => w);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<WalletDto>(It.IsAny<Wallet>()))
            .Returns(walletDto);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.PersonId.Should().Be(personId);
        result.Status.Should().Be("Active");

        _personRepositoryMock.Verify(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()), Times.Once);
        _walletRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PersonNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new CreateWalletCommand
        {
            PersonId = Guid.NewGuid(),
            Name = "My Digital Wallet",
            Description = "Primary wallet"
        };

        _tenantServiceMock.Setup(x => x.TenantId).Returns(Guid.NewGuid());
        _personRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Person?)null);

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Person*{command.PersonId}*");

        _walletRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateWalletName_ThrowsConflictException()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new CreateWalletCommand
        {
            PersonId = personId,
            Name = "Existing Wallet",
            Description = "Duplicate name test"
        };

        var personResult = Person.Create(
            "John",
            "Doe",
            "john.doe@example.com",
            "+61412345678");

        var person = personResult.Value;
        person.SetTenantId(tenantId.ToString());

        _tenantServiceMock.Setup(x => x.TenantId).Returns(tenantId);
        _personRepositoryMock.Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);
        _walletRepositoryMock.Setup(x => x.ExistsAsync(personId, command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already exists*");

        _walletRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}