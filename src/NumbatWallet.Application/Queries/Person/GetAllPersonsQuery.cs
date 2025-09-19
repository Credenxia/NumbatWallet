using NumbatWallet.Application.Commands.Person;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.SharedKernel.Results;

namespace NumbatWallet.Application.Queries.Person;

public sealed class GetAllPersonsQuery : IQuery<List<PersonDto>>
{
    public int? PageSize { get; init; }
    public int? PageNumber { get; init; }
}

public sealed class GetAllPersonsQueryHandler : IQueryHandler<GetAllPersonsQuery, List<PersonDto>>
{
    private readonly IPersonRepository _personRepository;

    public GetAllPersonsQueryHandler(IPersonRepository personRepository)
    {
        _personRepository = personRepository;
    }

    public async Task<Result<List<PersonDto>>> HandleAsync(GetAllPersonsQuery request, CancellationToken cancellationToken)
    {
        var persons = await _personRepository.GetAllAsync(cancellationToken);

        var dtos = persons.Select(person => new PersonDto
        {
            Id = person.Id,
            Email = person.Email.Value,
            PhoneNumber = person.PhoneNumber.Value,
            FirstName = person.FirstName,
            LastName = person.LastName,
            DateOfBirth = person.DateOfBirth,
            ExternalId = person.ExternalId,
            EmailVerificationStatus = person.EmailVerificationStatus.ToString(),
            PhoneVerificationStatus = person.PhoneVerificationStatus.ToString(),
            IsVerified = person.IsVerified,
            Status = person.Status.ToString(),
            CreatedAt = person.CreatedAt,
            UpdatedAt = person.UpdatedAt
        }).ToList();

        // Apply paging if specified
        if (request.PageSize.HasValue && request.PageNumber.HasValue)
        {
            var skip = (request.PageNumber.Value - 1) * request.PageSize.Value;
            dtos = dtos.Skip(skip).Take(request.PageSize.Value).ToList();
        }

        return dtos;
    }
}