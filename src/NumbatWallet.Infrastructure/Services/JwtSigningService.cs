using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

/// <summary>
/// Service for signing and verifying JWT-VC credentials
/// POA: Implementation for JWT-VC support
/// </summary>
public class JwtSigningService : IJwtSigningService
{
    private readonly IHsmService _hsmService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtSigningService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtSigningService(
        IHsmService hsmService,
        IConfiguration configuration,
        ILogger<JwtSigningService> logger)
    {
        _hsmService = hsmService;
        _configuration = configuration;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public async Task<string> SignCredentialAsync(
        Dictionary<string, object> credentialData,
        string? keyId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credentialData);

        try
        {
            // Get signing key from HSM or configuration
            var signingKey = await GetSigningKeyAsync(keyId, cancellationToken);

            // Build claims from credential data
            var claims = BuildClaims(credentialData);

            // Create token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = GetExpirationDate(credentialData),
                SigningCredentials = signingKey,
                IssuedAt = DateTime.UtcNow,
                NotBefore = DateTime.UtcNow,
                Issuer = _configuration["Jwt:Issuer"] ?? "https://numbatwallet.wa.gov.au"
            };

            // Add additional JWT headers
            if (credentialData.ContainsKey("id"))
            {
                tokenDescriptor.AdditionalHeaderClaims = new Dictionary<string, object>
                {
                    ["kid"] = keyId ?? "default",
                    ["typ"] = "JWT",
                    ["cty"] = "vc+ld+json"
                };
            }

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            var jwt = _tokenHandler.WriteToken(token);

            _logger.LogInformation("Successfully signed JWT-VC credential with key {KeyId}", keyId ?? "default");

            return jwt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign JWT-VC credential");
            throw new InvalidOperationException("Failed to sign credential", ex);
        }
    }

    public async Task<bool> VerifyCredentialAsync(
        string jwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return false;
        }

        try
        {
            // Parse token to get key ID
            var token = _tokenHandler.ReadJwtToken(jwt);
            var keyId = token.Header.Kid ?? "default";

            // Get verification key
            var verificationKey = await GetVerificationKeyAsync(keyId, cancellationToken);

            // Validate token
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = verificationKey,
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"] ?? "https://numbatwallet.wa.gov.au",
                ValidateAudience = false, // VCs typically don't have audience
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            _tokenHandler.ValidateToken(jwt, validationParameters, out _);

            _logger.LogInformation("Successfully verified JWT-VC credential");
            return true;
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger.LogWarning(ex, "JWT-VC credential validation failed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify JWT-VC credential");
            return false;
        }
    }

    public async Task<Dictionary<string, object>> DecodeCredentialAsync(
        string jwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            throw new ArgumentNullException(nameof(jwt));
        }

        try
        {
            var token = _tokenHandler.ReadJwtToken(jwt);
            var result = new Dictionary<string, object>();

            // Extract VC claims
            foreach (var claim in token.Claims)
            {
                if (claim.Type.StartsWith("vc.", StringComparison.Ordinal))
                {
                    var key = claim.Type.Substring(3);
                    try
                    {
                        result[key] = JsonSerializer.Deserialize<object>(claim.Value) ?? claim.Value;
                    }
                    catch
                    {
                        result[key] = claim.Value;
                    }
                }
            }

            // Add standard JWT claims
            result["iss"] = token.Issuer;
            result["sub"] = token.Subject;
            result["iat"] = token.IssuedAt;
            result["exp"] = token.ValidTo;
            result["nbf"] = token.ValidFrom;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decode JWT-VC credential");
            throw new InvalidOperationException("Failed to decode credential", ex);
        }
    }

    private async Task<SigningCredentials> GetSigningKeyAsync(
        string? keyId,
        CancellationToken cancellationToken)
    {
        // Try to get key from HSM first
        if (!string.IsNullOrEmpty(keyId))
        {
            try
            {
                // TODO: Replace with actual HSM key retrieval through IHsmService
                var keyName = $"jwt-signing-{keyId}";
                // For now, create a mock HsmKey object
                var hsmKey = new HsmKey
                {
                    Name = keyName,
                    Type = NumbatWallet.Domain.Interfaces.KeyType.RSA,
                    KeySize = 2048
                };
                if (hsmKey != null)
                {
                    // For RSA keys from HSM
                    if (hsmKey.Type == NumbatWallet.Domain.Interfaces.KeyType.RSA)
                    {
                        // For HSM keys, we typically can't export the private key
                        // So we'd need to use the HSM provider for signing operations
                        // For now, fall back to configuration key
                        _logger.LogWarning("HSM key found but cannot export private key, using configuration key");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get key {KeyId} from HSM, falling back to configuration", keyId);
            }
        }

        // Fall back to configuration-based key
        var secretKey = _configuration["Jwt:SecretKey"] ?? GenerateDefaultKey();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        return new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    private async Task<SecurityKey> GetVerificationKeyAsync(
        string keyId,
        CancellationToken cancellationToken)
    {
        // Try to get key from HSM first
        try
        {
            // TODO: Replace with actual HSM key retrieval through IHsmService
            var keyName = $"jwt-signing-{keyId}";
            // For now, create a mock HsmKey object
            var hsmKey = new HsmKey
            {
                Name = keyName,
                Type = NumbatWallet.Domain.Interfaces.KeyType.RSA,
                KeySize = 2048
            };
            if (hsmKey != null && hsmKey.Type == NumbatWallet.Domain.Interfaces.KeyType.RSA)
            {
                // For verification, we would typically use the public key from HSM
                // But HsmKey doesn't expose the public key directly
                _logger.LogWarning("HSM key found but public key not accessible, using configuration key");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get verification key {KeyId} from HSM", keyId);
        }

        // Fall back to configuration-based key
        var secretKey = _configuration["Jwt:SecretKey"] ?? GenerateDefaultKey();
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    }

    private List<Claim> BuildClaims(Dictionary<string, object> credentialData)
    {
        var claims = new List<Claim>();

        // Add standard VC claims with proper namespace
        if (credentialData.ContainsKey("@context"))
        {
            claims.Add(new Claim("vc.@context", JsonSerializer.Serialize(credentialData["@context"])));
        }

        if (credentialData.ContainsKey("type"))
        {
            claims.Add(new Claim("vc.type", JsonSerializer.Serialize(credentialData["type"])));
        }

        if (credentialData.ContainsKey("credentialSubject"))
        {
            claims.Add(new Claim("vc.credentialSubject", JsonSerializer.Serialize(credentialData["credentialSubject"])));
        }

        if (credentialData.ContainsKey("issuanceDate"))
        {
            claims.Add(new Claim("vc.issuanceDate", JsonSerializer.Serialize(credentialData["issuanceDate"])));
        }

        if (credentialData.ContainsKey("expirationDate"))
        {
            claims.Add(new Claim("vc.expirationDate", JsonSerializer.Serialize(credentialData["expirationDate"])));
        }

        // Add additional claims
        var standardClaims = new[] { "@context", "type", "credentialSubject", "issuanceDate", "expirationDate" };
        foreach (var kvp in credentialData.Where(x => !standardClaims.Contains(x.Key)))
        {
            claims.Add(new Claim($"vc.{kvp.Key}", JsonSerializer.Serialize(kvp.Value)));
        }

        return claims;
    }

    private DateTime GetExpirationDate(Dictionary<string, object> credentialData)
    {
        if (credentialData.ContainsKey("expirationDate"))
        {
            if (credentialData["expirationDate"] is DateTime expDate)
            {
                return expDate;
            }

            if (DateTime.TryParse(credentialData["expirationDate"]?.ToString(), out var parsed))
            {
                return parsed;
            }
        }

        // Default to 1 year expiration
        return DateTime.UtcNow.AddYears(1);
    }

    private string GenerateDefaultKey()
    {
        // Generate a secure random key for development/testing
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32]; // 256 bits
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

