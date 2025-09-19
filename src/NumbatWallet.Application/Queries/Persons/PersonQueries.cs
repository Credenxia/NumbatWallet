using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Queries.Persons;

public record GetAllPersonsQuery(int Page = 1, int PageSize = 20) : IQuery<IEnumerable<PersonDto>>;

public record GetPersonByIdQuery(Guid PersonId) : IQuery<PersonDto?>;

public record GetPersonByEmailQuery(string Email) : IQuery<PersonDto?>;

public record GetPersonByUserIdQuery(string UserId) : IQuery<PersonDto?>;

public record SearchPersonsQuery(
    string? SearchTerm,
    bool? IsVerified,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<IEnumerable<PersonDto>>;

public record GetPersonStatisticsQuery(Guid PersonId) : IQuery<object>;