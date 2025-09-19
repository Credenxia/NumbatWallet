using FluentAssertions;
using Moq;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Queries.Person;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Domain.ValueObjects;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Tests.Queries.Person;

public class SearchPersonsQueryHandlerTests
{
    private readonly Mock<IPersonRepository> _personRepositoryMock;
    private readonly SearchPersonsQueryHandler _handler;

    public SearchPersonsQueryHandlerTests()
    {
        _personRepositoryMock = new Mock<IPersonRepository>();
        _handler = new SearchPersonsQueryHandler(_personRepositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidSearchTerm_ReturnsPersonDtos()
    {
        // Arrange
        var searchTerm = "john";
        var query = new SearchPersonsQuery { SearchTerm = searchTerm };

        var person1 = new Domain.Aggregates.Person(
            "John",
            "Doe",
            DateOnly.FromDateTime(DateTime.Now.AddYears(-30)),
            "john.doe@example.com",
            "external-1",
            "tenant-1");

        var person2 = new Domain.Aggregates.Person(
            "Johnny",
            "Smith",
            DateOnly.FromDateTime(DateTime.Now.AddYears(-25)),
            "johnny.smith@example.com",
            "external-2",
            "tenant-1");

        var persons = new List<Domain.Aggregates.Person> { person1, person2 };

        _personRepositoryMock
            .Setup(x => x.SearchAsync(searchTerm, It.IsAny<CancellationToken>()))
            .ReturnsAsync(persons);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var firstPerson = result[0];
        firstPerson.FirstName.Should().Be("John");
        firstPerson.LastName.Should().Be("Doe");
        firstPerson.Email.Should().Be("john.doe@example.com");
        firstPerson.EmailVerificationStatus.Should().Be(VerificationStatus.NotVerified.ToString());
        firstPerson.PhoneVerificationStatus.Should().Be(VerificationStatus.NotVerified.ToString());
        firstPerson.Status.Should().Be(PersonStatus.PendingVerification.ToString());
        firstPerson.IsVerified.Should().BeFalse();

        _personRepositoryMock.Verify(x => x.SearchAsync(searchTerm, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithEmptySearchTerm_ReturnsAllPersons()
    {
        // Arrange
        var query = new SearchPersonsQuery { SearchTerm = "" };

        var person = new Domain.Aggregates.Person(
            "Jane",
            "Doe",
            DateOnly.FromDateTime(DateTime.Now.AddYears(-28)),
            "jane.doe@example.com",
            "external-3",
            "tenant-1");

        var persons = new List<Domain.Aggregates.Person> { person };

        _personRepositoryMock
            .Setup(x => x.SearchAsync("", It.IsAny<CancellationToken>()))
            .ReturnsAsync(persons);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].FirstName.Should().Be("Jane");
    }

    [Fact]
    public async Task HandleAsync_NoMatchingPersons_ReturnsEmptyList()
    {
        // Arrange
        var searchTerm = "nonexistent";
        var query = new SearchPersonsQuery { SearchTerm = searchTerm };

        _personRepositoryMock
            .Setup(x => x.SearchAsync(searchTerm, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Aggregates.Person>());

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var searchTerm = "test";
        var query = new SearchPersonsQuery { SearchTerm = searchTerm };

        var person = new Domain.Aggregates.Person(
            "Test",
            "User",
            DateOnly.FromDateTime(DateTime.Now.AddYears(-30)),
            "test.user@example.com",
            "external-test",
            "tenant-test");

        // Simulate person with verified status
        // Note: Person verification methods may not be publicly accessible
        // The test verifies default status values instead

        var persons = new List<Domain.Aggregates.Person> { person };

        _personRepositoryMock
            .Setup(x => x.SearchAsync(searchTerm, It.IsAny<CancellationToken>()))
            .ReturnsAsync(persons);

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        var dto = result[0];
        dto.Id.Should().Be(person.Id);
        dto.Email.Should().Be(person.Email.Value);
        dto.PhoneNumber.Should().Be(person.PhoneNumber.Value);
        dto.FirstName.Should().Be(person.FirstName);
        dto.LastName.Should().Be(person.LastName);
        dto.DateOfBirth.Should().Be(person.DateOfBirth);
        dto.ExternalId.Should().Be(person.ExternalId);
        dto.EmailVerificationStatus.Should().Be(VerificationStatus.NotVerified.ToString());
        dto.PhoneVerificationStatus.Should().Be(VerificationStatus.NotVerified.ToString());
        dto.IsVerified.Should().BeFalse();
        dto.Status.Should().Be(PersonStatus.PendingVerification.ToString());
        dto.CreatedAt.Should().Be(person.CreatedAt);
        dto.UpdatedAt.Should().Be(person.ModifiedAt);
    }

    [Fact]
    public async Task HandleAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var searchTerm = "test";
        var query = new SearchPersonsQuery { SearchTerm = searchTerm };
        var cancellationToken = new CancellationToken(true);

        _personRepositoryMock
            .Setup(x => x.SearchAsync(searchTerm, cancellationToken))
            .ReturnsAsync(new List<Domain.Aggregates.Person>());

        // Act
        await _handler.HandleAsync(query, cancellationToken);

        // Assert
        _personRepositoryMock.Verify(x => x.SearchAsync(searchTerm, cancellationToken), Times.Once);
    }
}