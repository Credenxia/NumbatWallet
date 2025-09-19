using FluentValidation;
using NumbatWallet.Application.Commands.Credentials;

namespace NumbatWallet.Application.Validators;

public class RevokeCredentialCommandValidator : AbstractValidator<RevokeCredentialCommand>
{
    public RevokeCredentialCommandValidator()
    {
        RuleFor(x => x.CredentialId)
            .NotEmpty().WithMessage("CredentialId is required")
            .Must(BeAValidGuid).WithMessage("CredentialId must be a valid GUID");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Revocation reason is required")
            .Length(5, 500).WithMessage("Revocation reason must be between 5 and 500 characters");
    }

    private bool BeAValidGuid(string? guid)
    {
        return Guid.TryParse(guid, out _);
    }
}