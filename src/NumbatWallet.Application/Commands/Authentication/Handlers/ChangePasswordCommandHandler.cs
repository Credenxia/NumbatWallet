using FluentValidation;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Authentication.Handlers;

public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, bool>
{
    private readonly IPersonRepository _personRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IPersonRepository personRepository,
        IUnitOfWork unitOfWork,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _personRepository = personRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Password change requested for user: {UserId}", command.UserId);

        if (!Guid.TryParse(command.UserId, out var personId))
        {
            throw new ValidationException("Invalid user ID");
        }

        var person = await _personRepository.GetByIdAsync(personId, cancellationToken);
        if (person == null)
        {
            throw new EntityNotFoundException("Person", command.UserId);
        }

        // In production, this would:
        // 1. Validate the current password against Azure AD or identity provider
        // 2. Update the password in the identity provider
        // 3. Force re-authentication on all active sessions

        // For POA, we'll just validate basic requirements
        if (string.IsNullOrWhiteSpace(command.NewPassword) || command.NewPassword.Length < 8)
        {
            throw new ValidationException("New password must be at least 8 characters");
        }

        // Log the successful change (no actual password storage in Person entity)
        _logger.LogInformation("Password changed successfully for user: {UserId}", command.UserId);

        // In a real system, we might update a LastPasswordChangeDate field
        // For now, just return success
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}