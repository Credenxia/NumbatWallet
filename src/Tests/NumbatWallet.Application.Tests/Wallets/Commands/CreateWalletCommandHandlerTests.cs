using Moq;
using FluentAssertions;
using NumbatWallet.Application.Wallets.Commands.CreateWallet;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Application.DomainServices;
using NumbatWallet.Domain.Aggregates;
using Microsoft.Extensions.Logging;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Tests.Wallets.Commands;

public class CreateWalletCommandHandlerTests
{
    private readonly Mock<IWalletRepository> _walletRepositoryMock;
    private readonly Mock<IPersonRepository> _personRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<CreateWalletCommandHandler>> _loggerMock;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<IWalletDomainService> _walletDomainServiceMock;
    private readonly Mock<IHsmService> _hsmServiceMock;
    private readonly CreateWalletCommandHandler _handler;

    public CreateWalletCommandHandlerTests()
    {
        _walletRepositoryMock = new Mock<IWalletRepository>();
        _personRepositoryMock = new Mock<IPersonRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<CreateWalletCommandHandler>>();
        _tenantServiceMock = new Mock<ITenantService>();
        _walletDomainServiceMock = new Mock<IWalletDomainService>();
        _hsmServiceMock = new Mock<IHsmService>();

        _handler = new CreateWalletCommandHandler(
            _walletRepositoryMock.Object,
            _personRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _tenantServiceMock.Object,
            _walletDomainServiceMock.Object,
            _hsmServiceMock.Object);
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
            Name = "My Digital Wallet"
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
            Id = expectedWallet.Id.ToString(),
            PersonId = personId.ToString(),
            PersonName = "John Doe",
            Name = command.Name,
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
        _hsmServiceMock.Setup(x => x.GenerateKeyPairAsync(It.IsAny<string>(), It.IsAny<KeyAlgorithm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-key-id");

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.PersonId.Should().Be(personId.ToString());
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
            Name = "My Digital Wallet"
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
            Name = "Existing Wallet"
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
        _walletRepositoryMock.Setup(x => x.WalletExistsForPersonAsync(personId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Person already has a wallet in this tenant*");

        _walletRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
