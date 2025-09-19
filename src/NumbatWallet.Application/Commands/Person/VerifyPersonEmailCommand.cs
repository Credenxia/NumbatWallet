using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.SharedKernel.Results;

namespace NumbatWallet.Application.Commands.Person;

public sealed class VerifyPersonEmailCommand : ICommand
{
    public Guid PersonId { get; init; }
    public string VerificationCode { get; init; } = string.Empty;
}

public sealed class VerifyPersonEmailCommandHandler : ICommandHandler<VerifyPersonEmailCommand>
{
    private readonly IPersonRepository _personRepository;

    public VerifyPersonEmailCommandHandler(IPersonRepository personRepository)
    {
        _personRepository = personRepository;
    }

    public async Task<Result> HandleAsync(VerifyPersonEmailCommand request, CancellationToken cancellationToken)
    {
        var person = await _personRepository.GetByIdAsync(request.PersonId, cancellationToken);
        if (person == null)
        {
            return DomainError.NotFound("Person.NotFound", $"Person with ID {request.PersonId} not found");
        }

        var result = person.VerifyEmail(request.VerificationCode);
        if (result.IsFailure)
        {
            return result;
        }

        _personRepository.Update(person);
        await _personRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}