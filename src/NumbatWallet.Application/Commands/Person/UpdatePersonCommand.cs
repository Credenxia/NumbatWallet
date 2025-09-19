using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.SharedKernel.Results;
using NumbatWallet.Application.Commands.Person;

namespace NumbatWallet.Application.Commands.Person;

public sealed class UpdatePersonCommand : ICommand
{
    public Guid Id { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? PhoneCountryCode { get; init; }
}

public sealed class UpdatePersonCommandHandler : ICommandHandler<UpdatePersonCommand>
{
    private readonly IPersonRepository _personRepository;

    public UpdatePersonCommandHandler(IPersonRepository personRepository)
    {
        _personRepository = personRepository;
    }

    public async Task<Result> HandleAsync(UpdatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = await _personRepository.GetByIdAsync(request.Id, cancellationToken);
        if (person == null)
        {
            return DomainError.NotFound("Person.NotFound", $"Person with ID {request.Id} not found");
        }

        // Update first name if provided
        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            var result = person.UpdateFirstName(request.FirstName);
            if (result.IsFailure)
                return result;
        }

        // Update last name if provided
        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            var result = person.UpdateLastName(request.LastName);
            if (result.IsFailure)
                return result;
        }

        // Update phone number if provided
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var result = person.UpdatePhoneNumber(request.PhoneNumber, request.PhoneCountryCode);
            if (result.IsFailure)
                return result;
        }

        _personRepository.Update(person);
        await _personRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}