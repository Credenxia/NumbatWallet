using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

/// <summary>
/// Production implementation of <see cref="IVerificationService"/>: issues REAL signed
/// presentation tokens (HS256 JWT using Jwt:SecretKey, same key/issuer/audience as the
/// access-token signer) and the public verification URL for them.
/// The token's <c>jti</c> is the persisted Presentation aggregate Id; validation happens in
/// Application.Commands.Presentations.Handlers.VerifyPresentationCommandHandler with
/// mirrored parameters.
/// NOTE: POA scope — signed-JWT presentation flow, not a W3C VP / OID4VP envelope.
/// </summary>
public sealed class JwtPresentationTokenService : IVerificationService
{
    private const string PresentationTokenType = "presentation";
    private const string DefaultIssuer = "https://numbatwallet.wa.gov.au";
    private const string DefaultAudience = "https://api.numbatwallet.wa.gov.au";
    private const string DefaultVerificationBaseUrl = "https://wallet.numbatwallet.com/verify";

    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtPresentationTokenService> _logger;

    public JwtPresentationTokenService(
        IConfiguration configuration,
        ILogger<JwtPresentationTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<string> CreatePresentationTokenAsync(
        Guid presentationId,
        Guid credentialId,
        string verifierId,
        string purpose,
        Dictionary<string, object> disclosedClaims,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(disclosedClaims);

        var now = DateTimeOffset.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, presentationId.ToString()),
            new("typ", PresentationTokenType),
            new("credential_id", credentialId.ToString()),
            new("verifier_id", verifierId),
            new("purpose", purpose),
            new("disclosed_claims", JsonSerializer.Serialize(disclosedClaims), JsonClaimValueTypes.Json)
        };

        var credentials = new SigningCredentials(GetKey(), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? DefaultIssuer,
            audience: _configuration["Jwt:Audience"] ?? DefaultAudience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        _logger.LogInformation(
            "Created presentation token {PresentationId} for credential {CredentialId} (expires {ExpiresAt})",
            presentationId, credentialId, expiresAt);

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    public Task<string> CreateVerificationUrlAsync(
        string presentationToken,
        CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["Presentation:VerificationBaseUrl"] ?? DefaultVerificationBaseUrl;
        return Task.FromResult($"{baseUrl.TrimEnd('/')}/{presentationToken}");
    }

    private SymmetricSecurityKey GetKey()
    {
        // SECURITY: never fall back to a hardcoded secret — fail fast if unconfigured.
        var secret = _configuration["Jwt:SecretKey"] ?? _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException(
                "JWT signing key is not configured. Set 'Jwt:SecretKey' (sourced from Key Vault in production).");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }
}
