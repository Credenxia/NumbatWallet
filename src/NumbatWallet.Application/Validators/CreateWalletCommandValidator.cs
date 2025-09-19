using FluentValidation;
using NumbatWallet.Application.Commands.Wallets;

namespace NumbatWallet.Application.Validators;

public class CreateWalletCommandValidator : AbstractValidator<CreateWalletCommand>
{
    public CreateWalletCommandValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty().WithMessage("PersonId is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Wallet name is required")
            .Length(1, 100).WithMessage("Wallet name must be between 1 and 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Wallet name contains invalid characters");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required")
            .MaximumLength(200).WithMessage("UserId cannot exceed 200 characters");
    }
}