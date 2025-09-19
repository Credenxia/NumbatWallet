using FluentValidation;
using NumbatWallet.Application.Commands.Wallets;

namespace NumbatWallet.Application.Validators;

public class DeleteWalletCommandValidator : AbstractValidator<DeleteWalletCommand>
{
    public DeleteWalletCommandValidator()
    {
        RuleFor(x => x.WalletId)
            .NotEmpty().WithMessage("WalletId is required")
            .Must(BeAValidGuid).WithMessage("WalletId must be a valid GUID");
    }

    private bool BeAValidGuid(string? guid)
    {
        return Guid.TryParse(guid, out _);
    }
}