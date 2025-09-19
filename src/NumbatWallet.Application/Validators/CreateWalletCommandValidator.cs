using FluentValidation;
using NumbatWallet.Application.Commands.Wallets;

namespace NumbatWallet.Application.Validators;

public class CreateWalletCommandValidator : AbstractValidator<CreateWalletCommand>
{
    public CreateWalletCommandValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty().WithMessage("PersonId is required")
            .Must(BeAValidGuid).WithMessage("PersonId must be a valid GUID");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Wallet name is required")
            .Length(1, 100).WithMessage("Wallet name must be between 1 and 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Wallet name contains invalid characters");

        When(x => x.Metadata != null, () =>
        {
            RuleFor(x => x.Metadata)
                .Must(x => x!.Count <= 50).WithMessage("Metadata cannot contain more than 50 items")
                .Must(x => x!.All(kvp => !string.IsNullOrWhiteSpace(kvp.Key)))
                    .WithMessage("Metadata keys cannot be empty")
                .Must(x => x!.All(kvp => kvp.Key.Length <= 50))
                    .WithMessage("Metadata keys cannot exceed 50 characters")
                .Must(x => x!.All(kvp => kvp.Value?.Length <= 500))
                    .WithMessage("Metadata values cannot exceed 500 characters");
        });
    }

    private bool BeAValidGuid(string? guid)
    {
        return Guid.TryParse(guid, out _);
    }
}