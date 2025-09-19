using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;
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
    private readonly IUnitOfWork _unitOfWork;

    public CreatePersonCommandHandler(IPersonRepository personRepository, IUnitOfWork unitOfWork)
    {
        _personRepository = personRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        // Check if person already exists
        var existingPerson = await _personRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingPerson != null)
        {
            throw new ConflictException("Person.EmailExists", $"Person with email {request.Email} already exists");
        }

        var personResult = Domain.Aggregates.Person.Create(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber);

        if (personResult.IsFailure)
        {
            throw new AppException(personResult.Error.Code, personResult.Error.Message);
        }

        await _personRepository.AddAsync(personResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return personResult.Value.Id;
    }
}