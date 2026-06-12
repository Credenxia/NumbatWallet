namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Creates verifier-facing presentation tokens and verification URLs.
/// The production implementation (Infrastructure: JwtPresentationTokenService) issues a
/// signed JWT whose <c>jti</c> equals the persisted Presentation aggregate Id, so the
/// verify-by-token flow can load the presentation record from the token.
/// NOTE: POA scope — signed-JWT presentation flow, not a W3C VP / OID4VP envelope.
/// </summary>
public interface IVerificationService
{
    /// <summary>
    /// Creates a signed presentation token.
    /// </summary>
    /// <param name="presentationId">Id of the persisted presentation; embedded as the token's <c>jti</c>.</param>
    /// <param name="credentialId">Id of the presented credential.</param>
    /// <param name="verifierId">Identifier of the verifier the credential is presented to.</param>
    /// <param name="purpose">Purpose of the presentation.</param>
    /// <param name="disclosedClaims">Only the claims disclosed to the verifier.</param>
    /// <param name="expiresAt">Token expiry; must match the presentation record's expiry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string> CreatePresentationTokenAsync(
        Guid presentationId,
        Guid credentialId,
        string verifierId,
        string purpose,
        Dictionary<string, object> disclosedClaims,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Builds the public URL a verifier can open/scan to verify the presentation token.
    /// </summary>
    Task<string> CreateVerificationUrlAsync(
        string presentationToken,
        CancellationToken cancellationToken);
}
