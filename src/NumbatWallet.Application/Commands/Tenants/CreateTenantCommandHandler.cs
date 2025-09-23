using FluentValidation;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Tenants;

/// <summary>
/// Handler for creating new tenants
/// POA: REAL IMPLEMENTATION - Not mock!
/// </summary>
public class CreateTenantCommandHandler : ICommandHandler<CreateTenantCommand, Guid>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateTenantCommand> _validator;
    private readonly ILogger<CreateTenantCommandHandler> _logger;

    public CreateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateTenantCommand> validator,
        ILogger<CreateTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Guid> HandleAsync(CreateTenantCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new tenant: {TenantName} with identifier: {Identifier}",
            command.Name, command.Identifier);

        // Validate command
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogError("Validation failed for CreateTenantCommand: {Errors}", errors);
            throw new ValidationException(validationResult.Errors);
        }

        // Check if tenant with same identifier already exists
        var existingTenant = await _tenantRepository.GetByIdentifierAsync(command.Identifier, cancellationToken);
        if (existingTenant != null)
        {
            _logger.LogError("Tenant with identifier {Identifier} already exists", command.Identifier);
            throw new InvalidOperationException($"Tenant with identifier '{command.Identifier}' already exists");
        }

        // Create the tenant entity
        var tenant = new Tenant
        {
            Name = command.Name,
            Identifier = command.Identifier,
            IsActive = true,
            SubscriptionTier = command.SubscriptionTier,
            Settings = command.Settings ?? new Dictionary<string, object>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add default settings if not provided
        if (!tenant.Settings.ContainsKey("features"))
        {
            tenant.Settings["features"] = GetDefaultFeatures(command.SubscriptionTier);
        }

        if (!tenant.Settings.ContainsKey("maxUsers"))
        {
            tenant.Settings["maxUsers"] = GetMaxUsers(command.SubscriptionTier);
        }

        if (!tenant.Settings.ContainsKey("maxWallets"))
        {
            tenant.Settings["maxWallets"] = GetMaxWallets(command.SubscriptionTier);
        }

        // Save to repository
        await _tenantRepository.AddAsync(tenant, cancellationToken);

        // Commit the transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tenant created successfully with ID: {TenantId}", tenant.Id);

        // TODO: Raise domain event for tenant created
        // await _eventBus.PublishAsync(new TenantCreatedEvent(tenant.Id, tenant.Name));

        return tenant.Id;
    }

    private List<string> GetDefaultFeatures(string subscriptionTier)
    {
        return subscriptionTier.ToLowerInvariant() switch
        {
            "enterprise" => new List<string> { "all", "bulk-operations", "api-access", "custom-branding", "advanced-reporting" },
            "professional" => new List<string> { "standard", "bulk-operations", "api-access" },
            "basic" => new List<string> { "standard" },
            _ => new List<string> { "standard" }
        };
    }

    private int GetMaxUsers(string subscriptionTier)
    {
        return subscriptionTier.ToLowerInvariant() switch
        {
            "enterprise" => -1, // Unlimited
            "professional" => 500,
            "basic" => 100,
            _ => 50
        };
    }

    private int GetMaxWallets(string subscriptionTier)
    {
        return subscriptionTier.ToLowerInvariant() switch
        {
            "enterprise" => -1, // Unlimited
            "professional" => 1000,
            "basic" => 200,
            _ => 100
        };
    }
}

/// <summary>
/// Validator for CreateTenantCommand
/// </summary>
public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tenant name is required")
            .MaximumLength(200).WithMessage("Tenant name cannot exceed 200 characters");

        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage("Tenant identifier is required")
            .MaximumLength(100).WithMessage("Tenant identifier cannot exceed 100 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Tenant identifier can only contain lowercase letters, numbers, and hyphens");

        RuleFor(x => x.SubscriptionTier)
            .NotEmpty().WithMessage("Subscription tier is required")
            .Must(BeValidSubscriptionTier).WithMessage("Invalid subscription tier. Must be Basic, Professional, or Enterprise");
    }

    private bool BeValidSubscriptionTier(string tier)
    {
        var validTiers = new[] { "Basic", "Professional", "Enterprise" };
        return validTiers.Contains(tier, StringComparer.OrdinalIgnoreCase);
    }
}