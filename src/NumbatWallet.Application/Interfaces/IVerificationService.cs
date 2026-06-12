namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Everything the token service needs to mint a W3C VP-JWT with an embedded VC-JWT for one
/// presentation. The disclosed claims become the embedded credential's credentialSubject —
/// selective disclosure happens at VC-embedding time (full-redisclosure-style SD; SD-JWT is
/// out of scope for the pilot).
/// </summary>
/// <param name="PresentationId">Persisted Presentation aggregate Id; becomes the VP-JWT's <c>jti</c>.</param>
/// <param name="CredentialId">Presented credential's aggregate Id; becomes the VC-JWT's <c>jti</c> (urn:uuid form).</param>
/// <param name="WalletId">Holder wallet Id; becomes the VP's <c>iss</c> and the VC's <c>sub</c> (urn:uuid form).</param>
/// <param name="IssuerId">Credential issuer's organization Id; becomes the VC's <c>iss</c> (urn:uuid form).</param>
/// <param name="CredentialType">Credential type appended to the VC's <c>vc.type</c> array.</param>
/// <param name="CredentialIssuedAt">Credential issuance instant; the VC's <c>nbf</c>.</param>
/// <param name="CredentialExpiresAt">Credential expiry; the VC's <c>exp</c> (omitted when null).</param>
/// <param name="VerifierId">Verifier the presentation is for; the VP's <c>aud</c>.</param>
/// <param name="Purpose">Purpose of the presentation (private VP claim).</param>
/// <param name="DisclosedClaims">ONLY the selectively disclosed claims; the VC's credentialSubject.</param>
/// <param name="Nonce">Replay-protection nonce embedded in the VP.</param>
/// <param name="ExpiresAt">Presentation/VP expiry; must match the presentation record's expiry.</param>
public sealed record PresentationTokenRequest(
    Guid PresentationId,
    Guid CredentialId,
    Guid WalletId,
    Guid IssuerId,
    string CredentialType,
    DateTimeOffset CredentialIssuedAt,
    DateTimeOffset? CredentialExpiresAt,
    string VerifierId,
    string Purpose,
    Dictionary<string, object> DisclosedClaims,
    string Nonce,
    DateTimeOffset ExpiresAt);

/// <summary>
/// Creates verifier-facing presentation tokens and verification URLs.
/// The production implementation (Infrastructure: JwtPresentationTokenService) issues a
/// W3C-conformant JWT-VP: a signed JWT whose <c>vp</c> claim is a VerifiablePresentation
/// embedding the presented credential as a JWT-VC string, and whose <c>jti</c> equals the
/// persisted Presentation aggregate Id so the verify-by-token flow can load the record.
/// Signing uses the same <see cref="IAccessTokenSigner"/> selection model as access tokens
/// (HS256 in dev, RS256 via Key Vault elsewhere) so the algorithm can change without code change.
/// </summary>
public interface IVerificationService
{
    /// <summary>Creates a signed W3C VP-JWT presentation token.</summary>
    Task<string> CreatePresentationTokenAsync(
        PresentationTokenRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Builds the public URL a verifier can open/scan to verify the presentation token.
    /// </summary>
    Task<string> CreateVerificationUrlAsync(
        string presentationToken,
        CancellationToken cancellationToken);
}
