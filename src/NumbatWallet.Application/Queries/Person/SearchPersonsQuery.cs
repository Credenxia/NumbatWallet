using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Queries.Person;

public sealed class SearchPersonsQuery : IQuery<IReadOnlyList<PersonDto>>
{
    public string SearchTerm { get; init; } = string.Empty;
}

public sealed class SearchPersonsQueryHandler : IQueryHandler<SearchPersonsQuery, IReadOnlyList<PersonDto>>
{
    private readonly IPersonRepository _personRepository;

    public SearchPersonsQueryHandler(IPersonRepository personRepository)
    {
        _personRepository = personRepository;
    }

    public async Task<IReadOnlyList<PersonDto>> HandleAsync(SearchPersonsQuery request, CancellationToken cancellationToken)
    {
        var persons = await _personRepository.SearchAsync(request.SearchTerm, cancellationToken);

        var personDtos = persons.Select(p => new PersonDto
        {
            Id = p.Id,
            Email = p.Email.Value,
            PhoneNumber = p.PhoneNumber.Value,
            FirstName = p.FirstName,
            LastName = p.LastName,
            DateOfBirth = p.DateOfBirth,
            ExternalId = p.ExternalId,
            EmailVerificationStatus = p.EmailVerificationStatus,
            PhoneVerificationStatus = p.PhoneVerificationStatus,
            IsVerified = p.IsVerified,
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();

        return personDtos;
    }
}