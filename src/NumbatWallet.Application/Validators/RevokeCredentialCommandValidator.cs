using FluentValidation;
using NumbatWallet.Application.Commands.Credentials;

namespace NumbatWallet.Application.Validators;

public class RevokeCredentialCommandValidator : AbstractValidator<RevokeCredentialCommand>
{
    public RevokeCredentialCommandValidator()
    {
        RuleFor(x => x.CredentialId)
            .NotEmpty().WithMessage("CredentialId is required");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Revocation reason is required")
            .Length(5, 500).WithMessage("Revocation reason must be between 5 and 500 characters");

        RuleFor(x => x.RevokerId)
            .NotEmpty().WithMessage("RevokerId is required")
            .MaximumLength(200).WithMessage("RevokerId cannot exceed 200 characters");
    }
}