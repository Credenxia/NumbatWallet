using FluentValidation;

namespace NumbatWallet.Application.Wallets.Commands.CreateWallet;

public sealed class CreateWalletCommandValidator : AbstractValidator<CreateWalletCommand>
{
    public CreateWalletCommandValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty().WithMessage("PersonId is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Wallet name is required")
            .MaximumLength(100).WithMessage("Wallet name must not exceed 100 characters")
            .Matches("^[a-zA-Z0-9 ._-]+$").WithMessage("Wallet name contains invalid characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}