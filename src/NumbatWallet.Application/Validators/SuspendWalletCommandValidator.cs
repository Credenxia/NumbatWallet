using FluentValidation;
using NumbatWallet.Application.Commands.Wallets;

namespace NumbatWallet.Application.Validators;

public class SuspendWalletCommandValidator : AbstractValidator<SuspendWalletCommand>
{
    public SuspendWalletCommandValidator()
    {
        RuleFor(x => x.WalletId)
            .NotEmpty().WithMessage("WalletId is required")
            .Must(BeAValidGuid).WithMessage("WalletId must be a valid GUID");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Suspension reason is required")
            .Length(5, 500).WithMessage("Suspension reason must be between 5 and 500 characters");

        When(x => x.SuspendedUntil.HasValue, () =>
        {
            RuleFor(x => x.SuspendedUntil!.Value)
                .GreaterThan(DateTimeOffset.UtcNow)
                .WithMessage("Suspension end date must be in the future")
                .LessThan(DateTimeOffset.UtcNow.AddYears(1))
                .WithMessage("Suspension cannot exceed 1 year");
        });
    }

    private bool BeAValidGuid(string? guid)
    {
        return Guid.TryParse(guid, out _);
    }
}