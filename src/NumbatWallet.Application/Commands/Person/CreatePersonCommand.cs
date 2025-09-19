using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.SharedKernel.Results;

namespace NumbatWallet.Application.Commands.Person;

public sealed class CreatePersonCommand : ICommand<Guid>
{
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? PhoneCountryCode { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly DateOfBirth { get; init; }
    public string ExternalId { get; init; } = string.Empty;
}

public sealed class CreatePersonCommandHandler : ICommandHandler<CreatePersonCommand, Guid>
{
    private readonly IPersonRepository _personRepository;

    public CreatePersonCommandHandler(IPersonRepository personRepository)
    {
        _personRepository = personRepository;
    }

    public async Task<Result<Guid>> HandleAsync(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        // Check if person already exists
        var existingPerson = await _personRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingPerson != null)
        {
            return DomainError.Conflict("Person.EmailExists", $"Person with email {request.Email} already exists");
        }

        var personResult = Domain.Aggregates.Person.Create(
            request.Email,
            request.PhoneNumber,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.ExternalId,
            request.PhoneCountryCode);

        if (personResult.IsFailure)
        {
            return personResult.Error;
        }

        await _personRepository.AddAsync(personResult.Value, cancellationToken);
        await _personRepository.SaveChangesAsync(cancellationToken);

        return personResult.Value.Id;
    }
}

// IPersonRepository moved to separate file
public interface IPersonRepository
{
    Task<Domain.Aggregates.Person?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Domain.Aggregates.Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Domain.Aggregates.Person person, CancellationToken cancellationToken = default);
    void Update(Domain.Aggregates.Person person);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<List<Domain.Aggregates.Person>> GetAllAsync(CancellationToken cancellationToken = default);
}