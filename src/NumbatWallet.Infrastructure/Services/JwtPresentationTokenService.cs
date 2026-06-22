using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using NumbatWallet.Application.Common;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

/// <summary>
/// Production implementation of <see cref="IVerificationService"/>: issues a W3C-conformant
/// <b>JWT-VP</b> (VC Data Model v1.1, JWT mapping):
/// <list type="bullet">
/// <item>Registered claims: <c>iss</c>/<c>sub</c> = holder wallet (urn:uuid), <c>aud</c> = verifier id,
/// <c>jti</c> = the persisted Presentation aggregate Id, <c>nbf</c>/<c>exp</c> = presentation lifetime,
/// plus a replay-protection <c>nonce</c>.</item>
/// <item><c>vp</c> claim: <c>{"@context":["https://www.w3.org/2018/credentials/v1"],
/// "type":["VerifiablePresentation"],"verifiableCredential":["&lt;JWT-VC&gt;"]}</c>.</item>
/// <item>The embedded JWT-VC carries ONLY the selectively disclosed claims in
/// <c>vc.credentialSubject</c> — selective disclosure happens at VC-embedding time. This is
/// full-redisclosure-style selective disclosure (the platform re-signs a derived credential);
/// SD-JWT (hash-based holder disclosure) is deliberately out of scope for the pilot.</item>
/// </list>
/// Both tokens are signed via <see cref="IAccessTokenSigner"/>, i.e. the same key/algorithm
/// selection model as access tokens: HS256 in dev, RS256 from Key Vault when configured —
/// switching algorithms requires no code change here.
/// Validation happens in Application.Commands.Presentations.Handlers.VerifyPresentationCommandHandler
/// using the signer's validation keys.
/// </summary>
public sealed class JwtPresentationTokenService : IVerificationService
{
    private const string DefaultVerificationBaseUrl = "https://wallet.numbatwallet.com/verify";

    private readonly IAccessTokenSigner _tokenSigner;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtPresentationTokenService> _logger;

    public JwtPresentationTokenService(
        IAccessTokenSigner tokenSigner,
        IConfiguration configuration,
        ILogger<JwtPresentationTokenService> logger)
    {
        _tokenSigner = tokenSigner;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<string> CreatePresentationTokenAsync(
        PresentationTokenRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTimeOffset.UtcNow;
        var vcJwt = CreateCredentialJwt(request, now);

        var vp = new Dictionary<string, object>
        {
            ["@context"] = new[] { W3cVcConstants.CredentialsContextV1 },
            ["type"] = new[] { W3cVcConstants.VerifiablePresentationType },
            ["verifiableCredential"] = new[] { vcJwt }
        };

        var holder = W3cVcConstants.ToUrnUuid(request.WalletId);
        var claims = new List<Claim>
        {
            // jti stays the bare presentation Guid: it is the persisted record's Id and the
            // public token/record contract (Id == jti) must not change.
            new(JwtRegisteredClaimNames.Jti, request.PresentationId.ToString()),
            new(JwtRegisteredClaimNames.Sub, holder),
            new(W3cVcConstants.NonceClaim, request.Nonce),
            new(W3cVcConstants.PurposeClaim, request.Purpose),
            new(W3cVcConstants.VpClaim, JsonSerializer.Serialize(vp), JsonClaimValueTypes.Json)
        };

        var token = _tokenSigner.CreateToken(
            claims,
            issuer: holder,
            audience: request.VerifierId,
            notBefore: now,
            expiresAt: request.ExpiresAt);

        _logger.LogInformation(
            "Created VP-JWT {PresentationId} ({Algorithm}) for credential {CredentialId} (verifier {VerifierId}, expires {ExpiresAt})",
            request.PresentationId, _tokenSigner.Algorithm, request.CredentialId, request.VerifierId, request.ExpiresAt);

        return Task.FromResult(token);
    }

    public Task<string> CreateVerificationUrlAsync(
        string presentationToken,
        CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["Presentation:VerificationBaseUrl"] ?? DefaultVerificationBaseUrl;
        return Task.FromResult($"{baseUrl.TrimEnd('/')}/{presentationToken}");
    }

    /// <summary>
    /// Builds the embedded JWT-VC: iss = credential issuer, sub = holder, jti = credential id,
    /// nbf = credential issuance, exp = credential expiry (omitted when the credential has none),
    /// vc = the W3C credential object whose credentialSubject contains ONLY the disclosed claims.
    /// </summary>
    private string CreateCredentialJwt(PresentationTokenRequest request, DateTimeOffset now)
    {
        var holder = W3cVcConstants.ToUrnUuid(request.WalletId);

        var credentialSubject = new Dictionary<string, object>(request.DisclosedClaims)
        {
            ["id"] = holder
        };

        var vc = new Dictionary<string, object>
        {
            ["@context"] = new[] { W3cVcConstants.CredentialsContextV1 },
            ["type"] = new[] { W3cVcConstants.VerifiableCredentialType, request.CredentialType },
            ["credentialSubject"] = credentialSubject
        };

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, W3cVcConstants.ToUrnUuid(request.CredentialId)),
            new(JwtRegisteredClaimNames.Sub, holder),
            new(W3cVcConstants.VcClaim, JsonSerializer.Serialize(vc), JsonClaimValueTypes.Json)
        };

        // nbf must not post-date the presentation: a credential issued "now" during tests can
        // carry an IssuedAt a few ms in the future of this call; clamp to now.
        var notBefore = request.CredentialIssuedAt < now ? request.CredentialIssuedAt : now;

        return _tokenSigner.CreateToken(
            claims,
            issuer: W3cVcConstants.ToUrnUuid(request.IssuerId),
            audience: null,
            notBefore: notBefore,
            expiresAt: request.CredentialExpiresAt);
    }
}
