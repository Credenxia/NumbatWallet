using FluentValidation;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Tenants;

/// <summary>
/// Handler for deleting tenants (soft delete)
/// POA: Real implementation
/// </summary>
public class DeleteTenantCommandHandler : ICommandHandler<DeleteTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<DeleteTenantCommand> _validator;
    private readonly ILogger<DeleteTenantCommandHandler> _logger;

    public DeleteTenantCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IValidator<DeleteTenantCommand> validator,
        ILogger<DeleteTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task HandleAsync(DeleteTenantCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting tenant {TenantId}", command.Id);

        // Validate command
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogError("Validation failed for DeleteTenantCommand: {Errors}", errors);
            throw new ValidationException(validationResult.Errors);
        }

        // Get the tenant
        var tenant = await _tenantRepository.GetByIdAsync(command.Id, cancellationToken);
        if (tenant == null)
        {
            _logger.LogError("Tenant {TenantId} not found", command.Id);
            throw new InvalidOperationException($"Tenant {command.Id} not found");
        }

        // Prevent deletion of master tenant
        if (tenant.Identifier == "master")
        {
            _logger.LogError("Cannot delete master tenant");
            throw new InvalidOperationException("Cannot delete the master tenant");
        }

        // Soft delete - just mark as inactive
        tenant.IsActive = false;
        tenant.UpdatedAt = DateTime.UtcNow;

        // Update in repository
        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tenant {TenantId} deleted (deactivated) successfully", command.Id);

        // TODO: Raise domain event
        // await _eventBus.PublishAsync(new TenantDeletedEvent(command.Id));
    }
}

/// <summary>
/// Validator for DeleteTenantCommand
/// </summary>
public class DeleteTenantCommandValidator : AbstractValidator<DeleteTenantCommand>
{
    public DeleteTenantCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Tenant ID is required")
            .NotEqual(Guid.Empty).WithMessage("Tenant ID cannot be empty GUID");
    }
}