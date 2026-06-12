using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Application.Services;

/// <summary>
/// Default access-token signer using a symmetric HS256 key from configuration.
/// Used for local/dev and as the fallback when asymmetric signing is not configured.
/// </summary>
public sealed class HmacAccessTokenSigner : IAccessTokenSigner
{
    private readonly IConfiguration _configuration;

    public HmacAccessTokenSigner(IConfiguration configuration) => _configuration = configuration;

    public string Algorithm => SecurityAlgorithms.HmacSha256;

    public string Issuer => _configuration["Jwt:Issuer"] ?? "https://numbatwallet.wa.gov.au";

    public string Audience => _configuration["Jwt:Audience"] ?? "https://api.numbatwallet.wa.gov.au";

    public string CreateToken(IEnumerable<Claim> claims, DateTimeOffset expiresAt)
    {
        var creds = new SigningCredentials(GetKey(), Algorithm);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateToken(
        IEnumerable<Claim> claims,
        string issuer,
        string? audience,
        DateTimeOffset notBefore,
        DateTimeOffset? expiresAt)
    {
        var creds = new SigningCredentials(GetKey(), Algorithm);
        // QUIRK: JwtSecurityToken silently drops nbf when expires is null, so for tokens
        // without an expiry (e.g. a VC for a never-expiring credential) nbf is added explicitly.
        var allClaims = expiresAt is null
            ? claims.Append(new Claim(
                JwtRegisteredClaimNames.Nbf,
                EpochTime.GetIntDate(notBefore.UtcDateTime).ToString(System.Globalization.CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64))
            : claims;
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: allClaims,
            notBefore: expiresAt is null ? null : notBefore.UtcDateTime,
            expires: expiresAt?.UtcDateTime,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public IReadOnlyList<SecurityKey> GetValidationKeys() => new SecurityKey[] { GetKey() };

    private SymmetricSecurityKey GetKey()
    {
        // SECURITY: never fall back to a hardcoded secret — fail fast if unconfigured.
        var secret = _configuration["Jwt:Key"] ?? _configuration["Jwt:SecretKey"];
        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException(
                "JWT signing key is not configured. Set 'Jwt:SecretKey' (sourced from Key Vault in production).");
        }
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }
}
