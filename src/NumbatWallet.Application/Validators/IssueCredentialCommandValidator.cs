using FluentValidation;
using NumbatWallet.Application.Commands.Credentials;

namespace NumbatWallet.Application.Validators;

public class IssueCredentialCommandValidator : AbstractValidator<IssueCredentialCommand>
{
    private readonly string[] _validCredentialTypes =
    {
        "DriverLicence",
        "ProofOfAge",
        "StudentCard",
        "HealthCard",
        "VaccinationCertificate",
        "WorkingWithChildrenCheck",
        "PoliceCheck"
    };

    public IssueCredentialCommandValidator()
    {
        RuleFor(x => x.HolderId)
            .NotEmpty().WithMessage("HolderId is required")
            .Must(BeAValidGuid).WithMessage("HolderId must be a valid GUID");

        RuleFor(x => x.IssuerId)
            .NotEmpty().WithMessage("IssuerId is required")
            .Must(BeAValidGuid).WithMessage("IssuerId must be a valid GUID");

        RuleFor(x => x.CredentialType)
            .NotEmpty().WithMessage("CredentialType is required")
            .Must(BeAValidCredentialType)
            .WithMessage($"CredentialType must be one of: {string.Join(", ", _validCredentialTypes)}");

        RuleFor(x => x.CredentialSubject)
            .NotNull().WithMessage("CredentialSubject is required")
            .Must(x => x != null && x.Count > 0).WithMessage("CredentialSubject cannot be empty")
            .Must(x => x == null || x.Count <= 100).WithMessage("CredentialSubject cannot contain more than 100 properties");

        When(x => x.ExpirationDate.HasValue, () =>
        {
            RuleFor(x => x.ExpirationDate!.Value)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Expiration date must be in the future")
                .LessThan(DateTime.UtcNow.AddYears(10))
                .WithMessage("Expiration date cannot be more than 10 years in the future");
        });

        When(x => x.Metadata != null, () =>
        {
            RuleFor(x => x.Metadata)
                .Must(x => x!.Count <= 20).WithMessage("Metadata cannot contain more than 20 items");
        });
    }

    private bool BeAValidGuid(string? guid)
    {
        return Guid.TryParse(guid, out _);
    }

    private bool BeAValidCredentialType(string credentialType)
    {
        return _validCredentialTypes.Contains(credentialType);
    }
}