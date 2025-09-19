using System.Text.Json;

namespace NumbatWallet.Domain.Credentials;

/// <summary>
/// Validator for different credential formats
/// </summary>
public class CredentialFormatValidator
{
    private readonly JwtVcFormat _jwtFormat;
    private readonly JsonLdFormat _jsonLdFormat;

    public CredentialFormatValidator()
    {
        _jwtFormat = new JwtVcFormat();
        _jsonLdFormat = new JsonLdFormat();
    }

    public bool IsValidJwtVc(string credential)
    {
        if (string.IsNullOrWhiteSpace(credential))
        {
            return false;
        }

        // Check JWT structure (three parts separated by dots)
        var parts = credential.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        try
        {
            // Try to decode base64 parts
            foreach (var part in parts.Take(2)) // Don't validate signature part
            {
                var padding = part.Length % 4;
                if (padding > 0)
                {
                    var paddedPart = part + new string('=', 4 - padding);
                    Convert.FromBase64String(paddedPart);
                }
                else
                {
                    Convert.FromBase64String(part);
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsValidJsonLd(string credential)
    {
        if (string.IsNullOrWhiteSpace(credential))
        {
            return false;
        }

        try
        {
            var doc = JsonDocument.Parse(credential);
            var root = doc.RootElement;

            // Check for required JSON-LD fields
            if (!root.TryGetProperty("@context", out _))
            {
                return false;
            }

            if (!root.TryGetProperty("type", out _))
            {
                return false;
            }

            if (!root.TryGetProperty("credentialSubject", out _))
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}