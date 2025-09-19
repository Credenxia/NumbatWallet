using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Queries.Person;

public sealed class GetPersonByIdQuery : IQuery<PersonDto>
{
    public Guid Id { get; init; }
}

public sealed class GetPersonByIdQueryHandler : IQueryHandler<GetPersonByIdQuery, PersonDto>
{
    private readonly IPersonRepository _personRepository;

    public GetPersonByIdQueryHandler(IPersonRepository personRepository)
    {
        _personRepository = personRepository;
    }

    public async Task<PersonDto> HandleAsync(GetPersonByIdQuery request, CancellationToken cancellationToken)
    {
        var person = await _personRepository.GetByIdAsync(request.Id, cancellationToken);
        if (person == null)
        {
            throw new NotFoundException("Person.NotFound", $"Person with ID {request.Id} not found");
        }

        var dto = new PersonDto
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
            UpdatedAt = person.ModifiedAt
        };

        return dto;
    }
}