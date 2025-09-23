using FluentValidation;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Tenants;

/// <summary>
/// Handler for updating tenants
/// POA: Real implementation
/// </summary>
public class UpdateTenantCommandHandler : ICommandHandler<UpdateTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateTenantCommand> _validator;
    private readonly ILogger<UpdateTenantCommandHandler> _logger;

    public UpdateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateTenantCommand> validator,
        ILogger<UpdateTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task HandleAsync(UpdateTenantCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating tenant {TenantId}", command.Id);

        // Validate command
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogError("Validation failed for UpdateTenantCommand: {Errors}", errors);
            throw new ValidationException(validationResult.Errors);
        }

        // Get the tenant
        var tenant = await _tenantRepository.GetByIdAsync(command.Id, cancellationToken);
        if (tenant == null)
        {
            _logger.LogError("Tenant {TenantId} not found", command.Id);
            throw new InvalidOperationException($"Tenant {command.Id} not found");
        }

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(command.Name))
        {
            tenant.Name = command.Name;
        }

        if (command.IsActive.HasValue)
        {
            tenant.IsActive = command.IsActive.Value;
        }

        if (!string.IsNullOrWhiteSpace(command.SubscriptionTier))
        {
            tenant.SubscriptionTier = command.SubscriptionTier;
        }

        if (command.Settings != null)
        {
            // Merge settings
            foreach (var setting in command.Settings)
            {
                tenant.Settings[setting.Key] = setting.Value;
            }
        }

        tenant.UpdatedAt = DateTime.UtcNow;

        // Save changes
        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tenant {TenantId} updated successfully", command.Id);
    }
}

/// <summary>
/// Validator for UpdateTenantCommand
/// </summary>
public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Tenant ID is required");

        When(x => !string.IsNullOrWhiteSpace(x.Name), () =>
        {
            RuleFor(x => x.Name)
                .MaximumLength(200).WithMessage("Tenant name cannot exceed 200 characters");
        });

        When(x => !string.IsNullOrWhiteSpace(x.SubscriptionTier), () =>
        {
            RuleFor(x => x.SubscriptionTier)
                .Must(BeValidSubscriptionTier).WithMessage("Invalid subscription tier");
        });
    }

    private bool BeValidSubscriptionTier(string? tier)
    {
        if (string.IsNullOrWhiteSpace(tier))
        {
            return true;
        }
        var validTiers = new[] { "Basic", "Professional", "Enterprise" };
        return validTiers.Contains(tier, StringComparer.OrdinalIgnoreCase);
    }
}