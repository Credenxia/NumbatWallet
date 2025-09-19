using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.SharedKernel.Results;

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

    public async Task<Result<PersonDto>> HandleAsync(GetPersonByIdQuery request, CancellationToken cancellationToken)
    {
        var person = await _personRepository.GetByIdAsync(request.Id, cancellationToken);
        if (person == null)
        {
            return DomainError.NotFound("Person.NotFound", $"Person with ID {request.Id} not found");
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
            UpdatedAt = person.UpdatedAt
        };

        return dto;
    }
}

public sealed class PersonDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly DateOfBirth { get; init; }
    public string ExternalId { get; init; } = string.Empty;
    public string EmailVerificationStatus { get; init; } = string.Empty;
    public string PhoneVerificationStatus { get; init; } = string.Empty;
    public bool IsVerified { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}