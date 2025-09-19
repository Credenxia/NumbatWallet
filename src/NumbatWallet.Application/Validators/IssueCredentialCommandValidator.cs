using FluentValidation;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Domain.Enums;

namespace NumbatWallet.Application.Validators;

public class IssueCredentialCommandValidator : AbstractValidator<IssueCredentialCommand>
{
    public IssueCredentialCommandValidator()
    {
        RuleFor(x => x.WalletId)
            .NotEmpty().WithMessage("WalletId is required");

        RuleFor(x => x.IssuerId)
            .NotEmpty().WithMessage("IssuerId is required");

        RuleFor(x => x.IssuerOrganizationId)
            .NotEmpty().WithMessage("IssuerOrganizationId is required");

        RuleFor(x => x.CredentialType)
            .IsInEnum().WithMessage("Invalid credential type");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required")
            .MaximumLength(500).WithMessage("Subject cannot exceed 500 characters");

        RuleFor(x => x.Claims)
            .NotNull().WithMessage("Claims are required")
            .Must(x => x != null && x.Count > 0).WithMessage("Claims cannot be empty")
            .Must(x => x == null || x.Count <= 100).WithMessage("Claims cannot contain more than 100 properties");

        RuleFor(x => x.ValidFrom)
            .NotEmpty().WithMessage("ValidFrom is required");

        When(x => x.ValidUntil.HasValue, () =>
        {
            RuleFor(x => x.ValidUntil!.Value)
                .GreaterThan(x => x.ValidFrom)
                .WithMessage("ValidUntil must be after ValidFrom")
                .LessThan(DateTime.UtcNow.AddYears(10))
                .WithMessage("ValidUntil cannot be more than 10 years in the future");
        });
    }
}