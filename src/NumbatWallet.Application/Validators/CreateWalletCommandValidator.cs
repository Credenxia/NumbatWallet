using FluentValidation;
using NumbatWallet.Application.Wallets.Commands.CreateWallet;

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

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid wallet type");

        RuleFor(x => x.TenantId)
            .MaximumLength(100).WithMessage("TenantId cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.TenantId));
    }
}
