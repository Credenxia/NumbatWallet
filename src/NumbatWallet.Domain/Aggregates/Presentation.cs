using NumbatWallet.SharedKernel.Attributes;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Guards;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Primitives;
using NumbatWallet.SharedKernel.Results;

namespace NumbatWallet.Domain.Aggregates;

/// <summary>
/// A credential presentation: the holder sharing (selectively disclosed) claims from a
/// credential with a verifier. The aggregate Id doubles as the <c>jti</c> of the signed
/// presentation token handed to the verifier, so a presentation can be looked up from the
/// token during the verify-by-token flow.
/// NOTE: POA scope — this models the signed-JWT presentation flow; the W3C VP / OID4VP
/// standards-grade envelope is out of scope for the pilot.
/// </summary>
public sealed class Presentation : AuditableEntity<Guid>, ITenantAware
{
    public Guid CredentialId { get; private set; }
    public Guid WalletId { get; private set; }
    public string TenantId { get; private set; } = string.Empty;

    [DataClassification(DataClassification.Official, "Presentation")]
    public string VerifierId { get; private set; }

    [DataClassification(DataClassification.Official, "Presentation")]
    public string Purpose { get; private set; }

    /// <summary>JSON object containing ONLY the claims disclosed to the verifier.</summary>
    [DataClassification(DataClassification.Protected, "Presentation")]
    public string DisclosedClaimsJson { get; private set; }

    public PresentationStatus Status { get; private set; }
    public DateTimeOffset PresentedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Timestamp of the most recent successful verification.</summary>
    public DateTimeOffset? VerifiedAt { get; private set; }

    /// <summary>Number of successful verifications (re-verification is allowed).</summary>
    public int VerificationCount { get; private set; }

    private Presentation() : base(Guid.Empty)
    {
        // Required for persistence - initialize non-nullable fields
        VerifierId = string.Empty;
        Purpose = string.Empty;
        DisclosedClaimsJson = string.Empty;
    }

    private Presentation(
        Guid credentialId,
        Guid walletId,
        string verifierId,
        string purpose,
        string disclosedClaimsJson,
        DateTimeOffset expiresAt)
        : base(Guid.NewGuid())
    {
        CredentialId = credentialId;
        WalletId = walletId;
        VerifierId = verifierId;
        Purpose = purpose;
        DisclosedClaimsJson = disclosedClaimsJson;
        Status = PresentationStatus.Pending;
        PresentedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
    }

    public static Result<Presentation> Create(
        Guid credentialId,
        Guid walletId,
        string verifierId,
        string purpose,
        string disclosedClaimsJson,
        DateTimeOffset expiresAt)
    {
        try
        {
            Guard.AgainstEmptyGuid(credentialId, nameof(credentialId));
            Guard.AgainstEmptyGuid(walletId, nameof(walletId));
            Guard.AgainstNullOrWhiteSpace(verifierId, nameof(verifierId));
            Guard.AgainstNullOrWhiteSpace(purpose, nameof(purpose));
            Guard.AgainstNullOrWhiteSpace(disclosedClaimsJson, nameof(disclosedClaimsJson));

            if (expiresAt <= DateTimeOffset.UtcNow)
            {
                return DomainError.Validation(
                    "Presentation.InvalidExpiry",
                    "Presentation expiry must be in the future.");
            }

            var presentation = new Presentation(
                credentialId,
                walletId,
                verifierId,
                purpose,
                disclosedClaimsJson,
                expiresAt);

            return Result.Success(presentation);
        }
        catch (ArgumentException ex)
        {
            return DomainError.Validation("Presentation.Invalid", ex.Message);
        }
    }

    public void SetTenantId(string tenantId)
    {
        Guard.AgainstNullOrWhiteSpace(tenantId, nameof(tenantId));
        TenantId = tenantId;
    }

    public bool IsExpired => ExpiresAt <= DateTimeOffset.UtcNow;

    /// <summary>
    /// Records a successful verification. Re-verification is allowed by design (a verifier
    /// may re-check the same token within its lifetime); each success increments
    /// <see cref="VerificationCount"/> and refreshes <see cref="VerifiedAt"/>.
    /// </summary>
    public Result MarkVerified()
    {
        if (Status == PresentationStatus.Expired || IsExpired)
        {
            return DomainError.BusinessRule("Presentation.Expired", "Cannot verify an expired presentation.");
        }

        if (Status == PresentationStatus.Rejected)
        {
            return DomainError.BusinessRule("Presentation.Rejected", "Cannot verify a rejected presentation.");
        }

        Status = PresentationStatus.Verified;
        VerifiedAt = DateTimeOffset.UtcNow;
        VerificationCount++;
        return Result.Success();
    }

    /// <summary>Marks the presentation as expired (idempotent).</summary>
    public Result MarkExpired()
    {
        Status = PresentationStatus.Expired;
        return Result.Success();
    }

    public Result Reject()
    {
        if (Status == PresentationStatus.Rejected)
        {
            return DomainError.BusinessRule("Presentation.AlreadyRejected", "Presentation is already rejected.");
        }

        Status = PresentationStatus.Rejected;
        return Result.Success();
    }
}
