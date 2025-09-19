using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Person;

public sealed class VerifyPersonEmailCommand : ICommand
{
    public Guid PersonId { get; init; }
    public string VerificationCode { get; init; } = string.Empty;
}

public sealed class VerifyPersonEmailCommandHandler : ICommandHandler<VerifyPersonEmailCommand>
{
    private readonly IPersonRepository _personRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyPersonEmailCommandHandler(IPersonRepository personRepository, IUnitOfWork unitOfWork)
    {
        _personRepository = personRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(VerifyPersonEmailCommand request, CancellationToken cancellationToken)
    {
        var person = await _personRepository.GetByIdAsync(request.PersonId, cancellationToken);
        if (person == null)
        {
            throw new NotFoundException("Person.NotFound", $"Person with ID {request.PersonId} not found");
        }

        var result = person.VerifyEmail(request.VerificationCode);
        if (result.IsFailure)
        {
            throw new AppException(result.Error.Code, result.Error.Message);
        }

        await _personRepository.UpdateAsync(person, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}