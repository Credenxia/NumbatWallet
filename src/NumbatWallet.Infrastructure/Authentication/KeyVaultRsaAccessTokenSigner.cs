using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Authentication;

/// <summary>
/// Asymmetric RS256 access-token signer. The RSA private key is sourced from Azure Key Vault
/// (a PEM secret) so issued tokens are verifiable by any party holding only the public key —
/// the property symmetric HS256 cannot provide and that W3C VC / TDIF require.
///
/// PROD HARDENING (tracked): the key is stored as a retrievable KV secret here for test/pilot.
/// Production should use KV-side signing (CryptographyClient, non-exportable key) or Managed HSM
/// so the private key never leaves the vault.
/// </summary>
public sealed class KeyVaultRsaAccessTokenSigner : IAccessTokenSigner
{
    private const string KeyId = "nw-jwt-rs256";
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeyVaultRsaAccessTokenSigner> _logger;
    private readonly Lazy<RsaSecurityKey> _key;

    public KeyVaultRsaAccessTokenSigner(IConfiguration configuration, ILogger<KeyVaultRsaAccessTokenSigner> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _key = new Lazy<RsaSecurityKey>(LoadKey, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string Algorithm => SecurityAlgorithms.RsaSha256; // RS256

    public string Issuer => _configuration["Jwt:Issuer"] ?? "https://numbatwallet.wa.gov.au";

    public string Audience => _configuration["Jwt:Audience"] ?? "https://api.numbatwallet.wa.gov.au";

    public string CreateToken(IEnumerable<Claim> claims, DateTimeOffset expiresAt)
    {
        var creds = new SigningCredentials(_key.Value, Algorithm);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public IReadOnlyList<SecurityKey> GetValidationKeys() => new SecurityKey[] { _key.Value };

    private RsaSecurityKey LoadKey()
    {
        var vaultUri = _configuration["Jwt:SigningKeyVaultUri"]
            ?? _configuration["KeyVault:Uri"]
            ?? throw new InvalidOperationException(
                "RSA signing is enabled but 'Jwt:SigningKeyVaultUri' (or 'KeyVault:Uri') is not configured.");
        var secretName = _configuration["Jwt:SigningKeySecretName"] ?? "jwt-signing-key-pem";

        _logger.LogInformation("Loading RSA JWT signing key from Key Vault {VaultUri} secret {SecretName}", vaultUri, secretName);

        var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
        var pem = client.GetSecret(secretName).Value.Value;

        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return new RsaSecurityKey(rsa) { KeyId = KeyId };
    }
}
