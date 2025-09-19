using FluentAssertions;
using Moq;
using NumbatWallet.Application.Commands.Person;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;
using Xunit;

namespace NumbatWallet.Application.Tests.Commands.Person;

public sealed class VerifyPersonPhoneCommandTests
{
    private readonly Mock<IPersonRepository> _mockPersonRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly VerifyPersonPhoneCommandHandler _handler;

    public VerifyPersonPhoneCommandTests()
    {
        _mockPersonRepository = new Mock<IPersonRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new VerifyPersonPhoneCommandHandler(_mockPersonRepository.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidCode_ShouldVerifyPhone()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var person = Domain.Aggregates.Person.Create(
            "John",
            "Doe",
            "john@example.com",
            "+61400000000").Value;

        // Simulate phone verification was requested
        var verificationCode = person.RequestPhoneVerification();

        var command = new VerifyPersonPhoneCommand
        {
            PersonId = personId,
            VerificationCode = verificationCode
        };

        _mockPersonRepository
            .Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        _mockPersonRepository.Verify(x => x.UpdateAsync(person, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCode_ShouldThrowException()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var person = Domain.Aggregates.Person.Create(
            "John",
            "Doe",
            "john@example.com",
            "+61400000000").Value;

        person.RequestPhoneVerification(); // Request verification

        var command = new VerifyPersonPhoneCommand
        {
            PersonId = personId,
            VerificationCode = "INVALID"
        };

        _mockPersonRepository
            .Setup(x => x.GetByIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        // Act
        var act = async () => await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*Invalid phone verification code*");
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentPerson_ShouldThrowNotFoundException()
    {
        // Arrange
        var command = new VerifyPersonPhoneCommand
        {
            PersonId = Guid.NewGuid(),
            VerificationCode = "123456"
        };

        _mockPersonRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Aggregates.Person?)null);

        // Act
        var act = async () => await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not found*");
    }
}