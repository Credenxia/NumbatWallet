using FluentValidation;
using NumbatWallet.Application.Commands.Credentials;

namespace NumbatWallet.Application.Validators;

public class ShareCredentialCommandValidator : AbstractValidator<ShareCredentialCommand>
{
    public ShareCredentialCommandValidator()
    {
        RuleFor(x => x.CredentialId)
            .NotEmpty().WithMessage("CredentialId is required");

        RuleFor(x => x.RecipientEmail)
            .NotEmpty().WithMessage("RecipientEmail is required")
            .EmailAddress().WithMessage("Invalid email address");

        RuleFor(x => x.ExpiresInMinutes)
            .GreaterThan(0).WithMessage("ExpiresInMinutes must be positive")
            .LessThanOrEqualTo(10080).WithMessage("Share link cannot be valid for more than 7 days");

        When(x => x.RequirePin, () =>
        {
            RuleFor(x => x.Pin)
                .NotEmpty().WithMessage("Pin is required when RequirePin is true")
                .Length(4, 8).WithMessage("Pin must be between 4 and 8 characters")
                .Matches(@"^\d+$").WithMessage("Pin must contain only digits");
        });
    }
}