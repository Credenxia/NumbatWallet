using FluentAssertions;
using Moq;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Queries.Person;
using NumbatWallet.Domain.Interfaces;
using Xunit;

namespace NumbatWallet.Application.Tests.Queries.Person;

public sealed class SearchPersonsQueryTests
{
    private readonly Mock<IPersonRepository> _mockPersonRepository;
    private readonly SearchPersonsQueryHandler _handler;

    public SearchPersonsQueryTests()
    {
        _mockPersonRepository = new Mock<IPersonRepository>();
        _handler = new SearchPersonsQueryHandler(_mockPersonRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidSearchTerm_ShouldReturnMatchingPersons()
    {
        // Arrange
        var person1 = Domain.Aggregates.Person.Create(
            "John",
            "Doe",
            "john@example.com",
            "+61400000000").Value;

        var person2 = Domain.Aggregates.Person.Create(
            "Jane",
            "Smith",
            "jane@example.com",
            "+61400000001").Value;

        var query = new SearchPersonsQuery
        {
            SearchTerm = "john"
        };

        _mockPersonRepository
            .Setup(x => x.SearchAsync(query.SearchTerm, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Aggregates.Person> { person1 });

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().FirstName.Should().Be("John");
        result.First().LastName.Should().Be("Doe");
        result.First().Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task HandleAsync_WithEmptySearchTerm_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new SearchPersonsQuery
        {
            SearchTerm = ""
        };

        _mockPersonRepository
            .Setup(x => x.SearchAsync(query.SearchTerm, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Aggregates.Person>());

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithNoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new SearchPersonsQuery
        {
            SearchTerm = "nonexistent"
        };

        _mockPersonRepository
            .Setup(x => x.SearchAsync(query.SearchTerm, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Aggregates.Person>());

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}