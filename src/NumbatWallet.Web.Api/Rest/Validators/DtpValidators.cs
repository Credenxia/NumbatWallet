using FluentValidation;
using NumbatWallet.Web.Api.Rest.Modules;

namespace NumbatWallet.Web.Api.Rest.Validators;

/// <summary>
/// Validator for DTP verify credential request
/// </summary>
public class DtpVerifyRequestValidator : AbstractValidator<DtpVerifyRequest>
{
    public DtpVerifyRequestValidator()
    {
        RuleFor(x => x.CredentialId)
            .NotEmpty().WithMessage("Credential ID is required")
            .Must(BeValidGuid).WithMessage("Credential ID must be a valid GUID");
    }

    private bool BeValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}

/// <summary>
/// Validator for DTP issue credential request
/// </summary>
public class DtpIssueRequestValidator : AbstractValidator<DtpIssueRequest>
{
    private readonly string[] _validCredentialTypes = new[]
    {
        "DriversLicense",
        "ProofOfAge",
        "SeniorCard",
        "StudentCard",
        "WorkingWithChildren",
        "ProofOfIdentity",
        "ProofOfResidence",
        "DigitalPassport"
    };

    public DtpIssueRequestValidator()
    {
        RuleFor(x => x.WalletId)
            .NotEmpty().WithMessage("Wallet ID is required")
            .Must(BeValidGuid).WithMessage("Wallet ID must be a valid GUID");

        RuleFor(x => x.CredentialType)
            .NotEmpty().WithMessage("Credential type is required")
            .Must(BeValidCredentialType).WithMessage($"Credential type must be one of: {string.Join(", ", _validCredentialTypes)}");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required")
            .MaximumLength(500).WithMessage("Subject must not exceed 500 characters");

        RuleFor(x => x.Claims)
            .NotNull().WithMessage("Claims are required")
            .Must(x => x != null && x.Count > 0).WithMessage("At least one claim is required")
            .Must(HaveValidClaims).WithMessage("Claims must contain valid key-value pairs");

        RuleFor(x => x.ExpirationDate)
            .Must(BeFutureDate).When(x => x.ExpirationDate.HasValue)
            .WithMessage("Expiration date must be in the future");
    }

    private bool BeValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }

    private bool BeValidCredentialType(string type)
    {
        return _validCredentialTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
    }

    private bool HaveValidClaims(Dictionary<string, object> claims)
    {
        if (claims == null)
        {
            return false;
        }

        foreach (var claim in claims)
        {
            if (string.IsNullOrWhiteSpace(claim.Key))
            {
                return false;
            }
            if (claim.Value == null)
            {
                return false;
            }
        }

        return true;
    }

    private bool BeFutureDate(DateTime? date)
    {
        return !date.HasValue || date.Value > DateTime.UtcNow;
    }
}

/// <summary>
/// Validator for DTP revoke credential request
/// </summary>
public class DtpRevokeRequestValidator : AbstractValidator<DtpRevokeRequest>
{
    private readonly string[] _validRevocationReasons = new[]
    {
        "Expired",
        "Suspended",
        "Fraudulent",
        "DataError",
        "UserRequest",
        "PolicyViolation",
        "SecurityConcern",
        "Other"
    };

    public DtpRevokeRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Revocation reason is required")
            .Must(BeValidReason).WithMessage($"Reason must be one of: {string.Join(", ", _validRevocationReasons)}");

        RuleFor(x => x.Details)
            .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Details))
            .WithMessage("Details must not exceed 1000 characters");
    }

    private bool BeValidReason(string reason)
    {
        return _validRevocationReasons.Contains(reason, StringComparer.OrdinalIgnoreCase);
    }
}