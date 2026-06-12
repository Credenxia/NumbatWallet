using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Signs API access tokens and exposes the keys needed to validate them. Abstracts the signing
/// algorithm/key source so the platform can move from symmetric (HS256, dev/default) to
/// asymmetric (RS256 from Key Vault) without changing the auth handlers.
/// </summary>
public interface IAccessTokenSigner
{
    /// <summary>JWS algorithm used, e.g. "HS256" or "RS256".</summary>
    string Algorithm { get; }

    /// <summary>Token issuer (iss).</summary>
    string Issuer { get; }

    /// <summary>Token audience (aud).</summary>
    string Audience { get; }

    /// <summary>Create a signed JWT for the given claims, expiring at <paramref name="expiresAt"/>.</summary>
    string CreateToken(IEnumerable<Claim> claims, DateTimeOffset expiresAt);

    /// <summary>
    /// Create a signed JWT with explicit registered claims, using the SAME key/algorithm
    /// selection as access tokens (HS256 dev default, RS256 via Key Vault elsewhere).
    /// Used for W3C VP-JWT / VC-JWT envelopes whose <c>iss</c> is the holder/issuer entity and
    /// whose <c>aud</c> is the verifier — not the platform's API issuer/audience.
    /// </summary>
    /// <param name="claims">Payload claims (jti, nonce, vp/vc, ...).</param>
    /// <param name="issuer">Token issuer (iss), e.g. the holder or credential-issuer identifier.</param>
    /// <param name="audience">Token audience (aud), e.g. the verifier id; null omits the claim.</param>
    /// <param name="notBefore">Token validity start (nbf).</param>
    /// <param name="expiresAt">Token expiry (exp); null omits the claim (e.g. a VC with no expiry).</param>
    string CreateToken(
        IEnumerable<Claim> claims,
        string issuer,
        string? audience,
        DateTimeOffset notBefore,
        DateTimeOffset? expiresAt);

    /// <summary>
    /// The key(s) a validator must use to verify tokens produced by <see cref="CreateToken"/>.
    /// KEY ROTATION: validators are configured from this list at startup, so during a key
    /// rotation an implementation must return BOTH the new and the previous key (distinguished
    /// by <c>kid</c>) for at least one access-token lifetime, then perform a rolling restart of
    /// the API instances; otherwise tokens signed with the old key are rejected immediately.
    /// </summary>
    IReadOnlyList<SecurityKey> GetValidationKeys();
}
