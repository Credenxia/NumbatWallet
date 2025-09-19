using FluentValidation;
using NumbatWallet.Application.Commands.Credentials;

namespace NumbatWallet.Application.Validators;

public class VerifyCredentialCommandValidator : AbstractValidator<VerifyCredentialCommand>
{
    public VerifyCredentialCommandValidator()
    {
        RuleFor(x => x.CredentialId)
            .NotEmpty().WithMessage("CredentialId is required")
            .Must(BeAValidGuid).WithMessage("CredentialId must be a valid GUID");

        When(x => !string.IsNullOrEmpty(x.VerifierId), () =>
        {
            RuleFor(x => x.VerifierId)
                .Must(BeAValidGuid).WithMessage("VerifierId must be a valid GUID when provided");
        });

        When(x => x.VerificationOptions != null, () =>
        {
            RuleFor(x => x.VerificationOptions)
                .Must(x => x!.Count <= 20).WithMessage("VerificationOptions cannot contain more than 20 items");
        });
    }

    private bool BeAValidGuid(string? guid)
    {
        return Guid.TryParse(guid, out _);
    }
}