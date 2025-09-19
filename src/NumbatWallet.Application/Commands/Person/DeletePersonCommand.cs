using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Person;

public sealed class DeletePersonCommand : ICommand
{
    public Guid Id { get; init; }
}

public sealed class DeletePersonCommandHandler : ICommandHandler<DeletePersonCommand>
{
    private readonly IPersonRepository _personRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePersonCommandHandler(IPersonRepository personRepository, IUnitOfWork unitOfWork)
    {
        _personRepository = personRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(DeletePersonCommand request, CancellationToken cancellationToken)
    {
        var person = await _personRepository.GetByIdAsync(request.Id, cancellationToken);
        if (person == null)
        {
            throw new NotFoundException("Person.NotFound", $"Person with ID {request.Id} not found");
        }

        // Check if person has wallets before deleting
        var personWithWallets = await _personRepository.GetWithWalletsAsync(request.Id, cancellationToken);
        if (personWithWallets?.Wallets.Any() == true)
        {
            throw new ConflictException("Person.HasWallets", "Cannot delete person with active wallets");
        }

        await _personRepository.DeleteAsync(person, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}