using FluentValidation;
using NumbatWallet.Application.Commands.Credentials;

namespace NumbatWallet.Application.Validators;

public class PresentCredentialCommandValidator : AbstractValidator<PresentCredentialCommand>
{
    public PresentCredentialCommandValidator()
    {
        RuleFor(x => x.CredentialId)
            .NotEmpty().WithMessage("CredentialId is required");

        RuleFor(x => x.VerifierId)
            .NotEmpty().WithMessage("VerifierId is required")
            .MaximumLength(200).WithMessage("VerifierId cannot exceed 200 characters");

        RuleFor(x => x.Purpose)
            .NotEmpty().WithMessage("Purpose is required")
            .MaximumLength(500).WithMessage("Purpose cannot exceed 500 characters");

        When(x => x.SelectiveDisclosure != null && x.SelectiveDisclosure.Count > 0, () =>
        {
            RuleFor(x => x.SelectiveDisclosure)
                .Must(x => x!.Count <= 50).WithMessage("Cannot selectively disclose more than 50 claims");
        });
    }
}