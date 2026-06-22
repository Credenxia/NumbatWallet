using NumbatWallet.SharedKernel.Attributes;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Guards;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Primitives;
using NumbatWallet.SharedKernel.Results;

namespace NumbatWallet.Domain.Aggregates;

/// <summary>
/// EXPERIMENTAL (minimal OID4VP subset): a verifier-initiated presentation request.
/// The verifier states WHAT it wants (credential type + claim names, expressed as a DIF
/// Presentation Exchange v2 definition) and receives a one-time <see cref="Nonce"/>; the
/// holder presents a VP-JWT carrying that nonce and submits it against this request.
/// Fulfilment is ONE-SHOT: the first valid submission moves the request to Fulfilled and
/// every later submission (replay) is rejected.
/// </summary>
public sealed class PresentationRequest : AuditableEntity<Guid>, ITenantAware
{
    public string TenantId { get; private set; } = string.Empty;

    [DataClassification(DataClassification.Official, "PresentationRequest")]
    public string VerifierId { get; private set; }

    [DataClassification(DataClassification.Official, "PresentationRequest")]
    public string Purpose { get; private set; }

    /// <summary>Credential type the verifier requires (e.g. "ProofOfAge").</summary>
    public string RequestedCredentialType { get; private set; }

    /// <summary>JSON array of the claim names the holder must disclose.</summary>
    public string RequestedClaimsJson { get; private set; }

    /// <summary>DIF Presentation Exchange v2 presentation_definition (JSON), built at creation.</summary>
    public string PresentationDefinitionJson { get; private set; }

    /// <summary>One-time nonce the submitted VP-JWT must carry (replay protection).</summary>
    public string Nonce { get; private set; }

    public PresentationRequestStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>When the request was fulfilled (one-shot).</summary>
    public DateTimeOffset? FulfilledAt { get; private set; }

    /// <summary>The presentation that fulfilled this request.</summary>
    public Guid? FulfilledByPresentationId { get; private set; }

    private PresentationRequest() : base(Guid.Empty)
    {
        // Required for persistence - initialize non-nullable fields
        VerifierId = string.Empty;
        Purpose = string.Empty;
        RequestedCredentialType = string.Empty;
        RequestedClaimsJson = string.Empty;
        PresentationDefinitionJson = string.Empty;
        Nonce = string.Empty;
    }

    private PresentationRequest(
        string verifierId,
        string purpose,
        string requestedCredentialType,
        string requestedClaimsJson,
        string presentationDefinitionJson,
        string nonce,
        DateTimeOffset expiresAt)
        : base(Guid.NewGuid())
    {
        VerifierId = verifierId;
        Purpose = purpose;
        RequestedCredentialType = requestedCredentialType;
        RequestedClaimsJson = requestedClaimsJson;
        PresentationDefinitionJson = presentationDefinitionJson;
        Nonce = nonce;
        Status = PresentationRequestStatus.Pending;
        ExpiresAt = expiresAt;
    }

    public static Result<PresentationRequest> Create(
        string verifierId,
        string purpose,
        string requestedCredentialType,
        string requestedClaimsJson,
        string presentationDefinitionJson,
        string nonce,
        DateTimeOffset expiresAt)
    {
        try
        {
            Guard.AgainstNullOrWhiteSpace(verifierId, nameof(verifierId));
            Guard.AgainstNullOrWhiteSpace(purpose, nameof(purpose));
            Guard.AgainstNullOrWhiteSpace(requestedCredentialType, nameof(requestedCredentialType));
            Guard.AgainstNullOrWhiteSpace(requestedClaimsJson, nameof(requestedClaimsJson));
            Guard.AgainstNullOrWhiteSpace(presentationDefinitionJson, nameof(presentationDefinitionJson));
            Guard.AgainstNullOrWhiteSpace(nonce, nameof(nonce));

            if (expiresAt <= DateTimeOffset.UtcNow)
            {
                return DomainError.Validation(
                    "PresentationRequest.InvalidExpiry",
                    "Presentation request expiry must be in the future.");
            }

            return Result.Success(new PresentationRequest(
                verifierId,
                purpose,
                requestedCredentialType,
                requestedClaimsJson,
                presentationDefinitionJson,
                nonce,
                expiresAt));
        }
        catch (ArgumentException ex)
        {
            return DomainError.Validation("PresentationRequest.Invalid", ex.Message);
        }
    }

    public void SetTenantId(string tenantId)
    {
        Guard.AgainstNullOrWhiteSpace(tenantId, nameof(tenantId));
        TenantId = tenantId;
    }

    public bool IsExpired => ExpiresAt <= DateTimeOffset.UtcNow;

    /// <summary>
    /// Fulfils the request with a verified presentation. ONE-SHOT by design: a request that
    /// is already Fulfilled (replay) or no longer Pending/within its lifetime fails.
    /// </summary>
    public Result MarkFulfilled(Guid presentationId)
    {
        if (Status == PresentationRequestStatus.Fulfilled)
        {
            return DomainError.BusinessRule(
                "PresentationRequest.AlreadyFulfilled",
                "Presentation request has already been fulfilled.");
        }

        if (Status == PresentationRequestStatus.Expired || IsExpired)
        {
            return DomainError.BusinessRule(
                "PresentationRequest.Expired",
                "Presentation request has expired.");
        }

        if (presentationId == Guid.Empty)
        {
            return DomainError.Validation(
                "PresentationRequest.InvalidPresentation",
                "A fulfilled request must reference the fulfilling presentation.");
        }

        Status = PresentationRequestStatus.Fulfilled;
        FulfilledAt = DateTimeOffset.UtcNow;
        FulfilledByPresentationId = presentationId;
        return Result.Success();
    }

    /// <summary>Marks the request as expired (idempotent; a fulfilled request stays fulfilled).</summary>
    public Result MarkExpired()
    {
        if (Status == PresentationRequestStatus.Fulfilled)
        {
            return DomainError.BusinessRule(
                "PresentationRequest.AlreadyFulfilled",
                "A fulfilled presentation request cannot expire.");
        }

        Status = PresentationRequestStatus.Expired;
        return Result.Success();
    }
}
