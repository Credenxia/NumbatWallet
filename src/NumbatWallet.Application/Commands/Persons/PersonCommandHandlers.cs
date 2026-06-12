using FluentValidation;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Application.Commands.Persons;

// Handlers for the Person command set consumed by the REST (PersonEndpoints) and GraphQL
// (Mutation) layers. They delegate to IPersonService so persistence/mapping live in one place.
//
// NOTE: a parallel command set exists under NumbatWallet.Application.Commands.Person
// (returning Guid). These are distinct closed generic types, so both register without
// collision. The two sets should be consolidated onto this one — see backlog.

public sealed class CreatePersonCommandHandler(IPersonService personService)
    : ICommandHandler<CreatePersonCommand, PersonDto>
{
    public Task<PersonDto> HandleAsync(CreatePersonCommand request, CancellationToken cancellationToken = default)
    {
        var dto = new CreatePersonDto
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            Address = request.Address
        };
        return personService.CreateAsync(dto, cancellationToken);
    }
}

public sealed class UpdatePersonCommandHandler(IPersonService personService)
    : ICommandHandler<UpdatePersonCommand, PersonDto>
{
    public Task<PersonDto> HandleAsync(UpdatePersonCommand request, CancellationToken cancellationToken = default)
    {
        var dto = new UpdatePersonDto
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            Address = request.Address
        };
        return personService.UpdateAsync(request.Id, dto, cancellationToken);
    }
}

public sealed class DeletePersonCommandHandler(IPersonService personService)
    : ICommandHandler<DeletePersonCommand, bool>
{
    public Task<bool> HandleAsync(DeletePersonCommand request, CancellationToken cancellationToken = default)
        => personService.DeleteAsync(request.PersonId, cancellationToken);
}

public sealed class CreatePersonCommandValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpdatePersonCommandValidator : AbstractValidator<UpdatePersonCommand>
{
    public UpdatePersonCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName is not null);
    }
}
