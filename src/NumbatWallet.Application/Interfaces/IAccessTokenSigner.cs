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
    /// The key(s) a validator must use to verify tokens produced by <see cref="CreateToken"/>.
    /// KEY ROTATION: validators are configured from this list at startup, so during a key
    /// rotation an implementation must return BOTH the new and the previous key (distinguished
    /// by <c>kid</c>) for at least one access-token lifetime, then perform a rolling restart of
    /// the API instances; otherwise tokens signed with the old key are rejected immediately.
    /// </summary>
    IReadOnlyList<SecurityKey> GetValidationKeys();
}
