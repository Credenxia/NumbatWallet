using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Domain.ValueObjects;
using NumbatWallet.SharedKernel.Interfaces;

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
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePersonCommandHandler(IPersonRepository personRepository, IUnitOfWork unitOfWork)
    {
        _personRepository = personRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(UpdatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = await _personRepository.GetByIdAsync(request.Id, cancellationToken);
        if (person == null)
        {
            throw new NotFoundException("Person.NotFound", $"Person with ID {request.Id} not found");
        }

        // Update phone number if provided
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phoneNumber = PhoneNumber.Create(request.PhoneNumber);
            var result = person.UpdatePhoneNumber(phoneNumber);
            if (result.IsFailure)
            {
                throw new AppException(result.Error.Code, result.Error.Message);
            }
        }

        await _personRepository.UpdateAsync(person, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
