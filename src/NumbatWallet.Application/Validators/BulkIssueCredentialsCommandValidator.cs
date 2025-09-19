using FluentValidation;
using NumbatWallet.Application.Commands.Credentials;

namespace NumbatWallet.Application.Validators;

public class BulkIssueCredentialsCommandValidator : AbstractValidator<BulkIssueCredentialsCommand>
{
    public BulkIssueCredentialsCommandValidator()
    {
        RuleFor(x => x.WalletIds)
            .NotNull().WithMessage("WalletIds is required")
            .NotEmpty().WithMessage("At least one wallet ID is required")
            .Must(x => x.Count <= 100).WithMessage("Cannot issue to more than 100 wallets at once");

        RuleFor(x => x.CredentialType)
            .IsInEnum().WithMessage("Invalid credential type");

        RuleFor(x => x.Template)
            .NotNull().WithMessage("Template is required")
            .Must(x => x != null && x.Count > 0).WithMessage("Template cannot be empty");

        RuleFor(x => x.IssuerId)
            .NotEmpty().WithMessage("IssuerId is required");

        RuleFor(x => x.IssuerOrganizationId)
            .NotEmpty().WithMessage("IssuerOrganizationId is required");

        RuleFor(x => x.ValidFrom)
            .NotEmpty().WithMessage("ValidFrom is required");

        When(x => x.ValidUntil.HasValue, () =>
        {
            RuleFor(x => x.ValidUntil!.Value)
                .GreaterThan(x => x.ValidFrom)
                .WithMessage("ValidUntil must be after ValidFrom");
        });
    }
}