using NumbatWallet.Application.Services;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Tests.Services;

/// <summary>
/// Regression: PersonService.SearchAsync used to build an EF predicate over the AES-encrypted
/// Email/FirstName/LastName columns (FindAsync with .Contains), which threw at query translation
/// and surfaced as HTTP 500 on /api/v1/persons/search. It must delegate to the repository's
/// SearchAsync (queryable fields only) and never touch FindAsync.
/// </summary>
public class PersonServiceSearchTests
{
    private readonly Mock<IPersonRepository> _personRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly PersonService _service;

    public PersonServiceSearchTests()
    {
        _service = new PersonService(_personRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task SearchAsync_Delegates_To_Repository_SearchAsync()
    {
        // Arrange
        var person = Person.Create("Test", "Citizen", "citizen@example.com", "+61400000000").Value;
        person.SetTenantId(Guid.NewGuid().ToString());

        _personRepositoryMock
            .Setup(x => x.SearchAsync("citizen@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Person> { person });

        // Act
        var results = await _service.SearchAsync("citizen@example.com");

        // Assert
        results.Should().ContainSingle();
        _personRepositoryMock.Verify(x => x.SearchAsync("citizen@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _personRepositoryMock.Verify(
            x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Person, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WithNoMatches_ReturnsEmpty()
    {
        _personRepositoryMock
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Person>());

        var results = await _service.SearchAsync("Citizen");

        results.Should().BeEmpty();
    }
}
