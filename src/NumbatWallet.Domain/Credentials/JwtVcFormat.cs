using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace NumbatWallet.Domain.Credentials;

/// <summary>
/// Implements JWT-VC format for W3C Verifiable Credentials
/// According to https://www.w3.org/TR/vc-data-model/#json-web-token
/// </summary>
public class JwtVcFormat : ICredentialFormat
{
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtVcFormat()
    {
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public string SerializeCredential(Dictionary<string, object> credentialData, string? signingKey = null)
    {
        if (credentialData == null)
            throw new ArgumentNullException(nameof(credentialData));

        // Use a default key if none provided (for testing) - must be at least 256 bits (32 bytes)
        var key = signingKey ?? "default-test-key-must-be-at-least-32-characters-long-for-HS256";

        // Ensure key is long enough for HS256 (minimum 256 bits = 32 bytes)
        if (Encoding.UTF8.GetByteCount(key) < 32)
        {
            // Pad the key to make it 32 bytes
            key = key.PadRight(32, 'X');
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>();

        // Add standard VC claims
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

        // Add additional claims
        foreach (var kvp in credentialData.Where(x => !new[] { "@context", "type", "credentialSubject" }.Contains(x.Key)))
        {
            claims.Add(new Claim($"vc.{kvp.Key}", JsonSerializer.Serialize(kvp.Value)));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddYears(1),
            SigningCredentials = credentials,
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    public Dictionary<string, object> DeserializeCredential(string serializedCredential)
    {
        if (string.IsNullOrWhiteSpace(serializedCredential))
            throw new ArgumentNullException(nameof(serializedCredential));

        try
        {
            var token = _tokenHandler.ReadJwtToken(serializedCredential);
            var result = new Dictionary<string, object>();

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

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to deserialize JWT-VC credential", ex);
        }
    }

    public bool IsValidFormat(string serializedCredential)
    {
        if (string.IsNullOrWhiteSpace(serializedCredential))
            return false;

        try
        {
            var parts = serializedCredential.Split('.');
            if (parts.Length != 3)
                return false;

            _tokenHandler.ReadJwtToken(serializedCredential);
            return true;
        }
        catch
        {
            return false;
        }
    }
}