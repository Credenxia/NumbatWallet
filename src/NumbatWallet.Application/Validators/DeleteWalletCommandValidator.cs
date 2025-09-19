using FluentValidation;
using NumbatWallet.Application.Commands.Wallets;

namespace NumbatWallet.Application.Validators;

public class DeleteWalletCommandValidator : AbstractValidator<DeleteWalletCommand>
{
    public DeleteWalletCommandValidator()
    {
        RuleFor(x => x.WalletId)
            .NotEmpty().WithMessage("WalletId is required");
    }
}