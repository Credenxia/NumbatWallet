using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Application.Queries.Persons;

// Handlers for the Person query set consumed by PersonEndpoints. They delegate to
// IPersonService. A parallel query set exists under Queries.Person (different result
// types); these are distinct closed generics and register without collision.

public sealed class GetAllPersonsQueryHandler(IPersonService personService)
    : IQueryHandler<GetAllPersonsQuery, IEnumerable<PersonDto>>
{
    public Task<IEnumerable<PersonDto>> HandleAsync(GetAllPersonsQuery query, CancellationToken cancellationToken = default)
        => personService.GetPagedAsync(query.Page, query.PageSize, cancellationToken);
}

public sealed class GetPersonByIdQueryHandler(IPersonService personService)
    : IQueryHandler<GetPersonByIdQuery, PersonDto?>
{
    public Task<PersonDto?> HandleAsync(GetPersonByIdQuery query, CancellationToken cancellationToken = default)
        => personService.GetByIdAsync(query.PersonId, cancellationToken);
}

public sealed class GetPersonByEmailQueryHandler(IPersonService personService)
    : IQueryHandler<GetPersonByEmailQuery, PersonDto?>
{
    public Task<PersonDto?> HandleAsync(GetPersonByEmailQuery query, CancellationToken cancellationToken = default)
        => personService.GetByEmailAsync(query.Email, cancellationToken);
}

public sealed class SearchPersonsQueryHandler(IPersonService personService)
    : IQueryHandler<SearchPersonsQuery, IEnumerable<PersonDto>>
{
    public async Task<IEnumerable<PersonDto>> HandleAsync(SearchPersonsQuery query, CancellationToken cancellationToken = default)
    {
        var results = await personService.SearchAsync(query.SearchTerm ?? string.Empty, cancellationToken);
        return results;
    }
}
