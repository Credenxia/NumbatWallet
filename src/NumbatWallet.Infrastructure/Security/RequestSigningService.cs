using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.ValueObjects;

namespace NumbatWallet.Infrastructure.Security;

public class RequestSigningService : IRequestSigningService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RequestSigningService> _logger;
    private readonly TimeSpan _nonceExpiration = TimeSpan.FromMinutes(5);

    public RequestSigningService(
        IDistributedCache cache,
        ILogger<RequestSigningService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> SignRequestAsync(
        string method,
        string path,
        string? body,
        Dictionary<string, string>? headers,
        string privateKey,
        string algorithm = "SHA256")
    {
        // Generate nonce
        var nonce = GenerateNonce();
        var timestamp = DateTimeOffset.UtcNow;

        // Create signature object
        var signature = new RequestSignature(
            algorithm,
            "", // Will be filled after computing
            nonce,
            timestamp,
            headers);

        // Get signature base
        var signatureBase = signature.GetSignatureBase(method, path, body);

        // Compute signature
        var signatureValue = ComputeSignature(signatureBase, privateKey, algorithm);

        // Return signature header value
        return FormatSignatureHeader(algorithm, signatureValue, nonce, timestamp);
    }

    public async Task<bool> VerifyRequestSignatureAsync(
        RequestSignature signature,
        string method,
        string path,
        string? body,
        string publicKey)
    {
        try
        {
            // Check if signature is expired
            if (signature.IsExpired())
            {
                _logger.LogWarning("Request signature expired. Timestamp: {Timestamp}", signature.Timestamp);
                return false;
            }

            // Check nonce for replay attack prevention
            if (!await ValidateNonceAsync(signature.Nonce))
            {
                _logger.LogWarning("Invalid or reused nonce: {Nonce}", signature.Nonce);
                return false;
            }

            // Get signature base
            var signatureBase = signature.GetSignatureBase(method, path, body);

            // Verify signature
            var isValid = VerifySignature(signatureBase, signature.Signature, publicKey, signature.Algorithm);

            if (isValid)
            {
                // Mark nonce as used
                await MarkNonceAsUsedAsync(signature.Nonce);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying request signature");
            return false;
        }
    }

    public string GenerateNonce()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public async Task<bool> ValidateNonceAsync(string nonce)
    {
        var key = $"nonce:{nonce}";
        var value = await _cache.GetStringAsync(key);

        // If nonce exists in cache, it's already been used
        return string.IsNullOrEmpty(value);
    }

    public async Task MarkNonceAsUsedAsync(string nonce)
    {
        var key = $"nonce:{nonce}";
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _nonceExpiration
        };

        await _cache.SetStringAsync(key, DateTimeOffset.UtcNow.ToString(), options);
    }

    private string ComputeSignature(string data, string privateKey, string algorithm)
    {
        byte[] signatureBytes;

        switch (algorithm.ToUpperInvariant())
        {
            case "SHA256":
            case "RSA-SHA256":
                using (var sha256 = SHA256.Create())
                {
                    var dataBytes = Encoding.UTF8.GetBytes(data);
                    var hashBytes = SHA256.HashData(dataBytes);

                    if (algorithm.StartsWith("RSA", StringComparison.OrdinalIgnoreCase))
                    {
                        // RSA signature
                        using var rsa = RSA.Create();
                        rsa.ImportFromPem(privateKey);
                        signatureBytes = rsa.SignHash(hashBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    }
                    else
                    {
                        // HMAC signature
                        using var hmac = new HMACSHA256(Convert.FromBase64String(privateKey));
                        signatureBytes = hmac.ComputeHash(dataBytes);
                    }
                }
                break;

            case "SHA384":
                using (var sha384 = SHA384.Create())
                {
                    var dataBytes = Encoding.UTF8.GetBytes(data);
                    var hashBytes = SHA384.HashData(dataBytes);

                    using var hmac = new HMACSHA384(Convert.FromBase64String(privateKey));
                    signatureBytes = hmac.ComputeHash(dataBytes);
                }
                break;

            case "SHA512":
            case "RSA-SHA512":
                using (var sha512 = SHA512.Create())
                {
                    var dataBytes = Encoding.UTF8.GetBytes(data);
                    var hashBytes = SHA512.HashData(dataBytes);

                    if (algorithm.StartsWith("RSA", StringComparison.OrdinalIgnoreCase))
                    {
                        // RSA signature
                        using var rsa = RSA.Create();
                        rsa.ImportFromPem(privateKey);
                        signatureBytes = rsa.SignHash(hashBytes, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
                    }
                    else
                    {
                        // HMAC signature
                        using var hmac = new HMACSHA512(Convert.FromBase64String(privateKey));
                        signatureBytes = hmac.ComputeHash(dataBytes);
                    }
                }
                break;

            default:
                throw new NotSupportedException($"Algorithm {algorithm} is not supported");
        }

        return Convert.ToBase64String(signatureBytes);
    }

    private bool VerifySignature(string data, string signature, string publicKey, string algorithm)
    {
        try
        {
            var signatureBytes = Convert.FromBase64String(signature);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            switch (algorithm.ToUpperInvariant())
            {
                case "SHA256":
                case "RSA-SHA256":
                    using (var sha256 = SHA256.Create())
                    {
                        var hashBytes = SHA256.HashData(dataBytes);

                        if (algorithm.StartsWith("RSA", StringComparison.OrdinalIgnoreCase))
                        {
                            // RSA verification
                            using var rsa = RSA.Create();
                            rsa.ImportFromPem(publicKey);
                            return rsa.VerifyHash(hashBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                        }
                        else
                        {
                            // HMAC verification
                            using var hmac = new HMACSHA256(Convert.FromBase64String(publicKey));
                            var expectedSignature = hmac.ComputeHash(dataBytes);
                            return signatureBytes.SequenceEqual(expectedSignature);
                        }
                    }

                case "SHA384":
                    using (var hmac = new HMACSHA384(Convert.FromBase64String(publicKey)))
                    {
                        var expectedSignature = hmac.ComputeHash(dataBytes);
                        return signatureBytes.SequenceEqual(expectedSignature);
                    }

                case "SHA512":
                case "RSA-SHA512":
                    using (var sha512 = SHA512.Create())
                    {
                        var hashBytes = SHA512.HashData(dataBytes);

                        if (algorithm.StartsWith("RSA", StringComparison.OrdinalIgnoreCase))
                        {
                            // RSA verification
                            using var rsa = RSA.Create();
                            rsa.ImportFromPem(publicKey);
                            return rsa.VerifyHash(hashBytes, signatureBytes, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
                        }
                        else
                        {
                            // HMAC verification
                            using var hmac = new HMACSHA512(Convert.FromBase64String(publicKey));
                            var expectedSignature = hmac.ComputeHash(dataBytes);
                            return signatureBytes.SequenceEqual(expectedSignature);
                        }
                    }

                default:
                    _logger.LogWarning("Unsupported algorithm: {Algorithm}", algorithm);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signature");
            return false;
        }
    }

    private string FormatSignatureHeader(string algorithm, string signature, string nonce, DateTimeOffset timestamp)
    {
        return $"Signature algorithm=\"{algorithm}\",signature=\"{signature}\",nonce=\"{nonce}\",timestamp=\"{timestamp.ToUnixTimeSeconds()}\"";
    }

    public RequestSignature? ParseSignatureHeader(string headerValue)
    {
        try
        {
            var parts = new Dictionary<string, string>();
            var keyValuePairs = headerValue.Replace("Signature ", "").Split(',');

            foreach (var pair in keyValuePairs)
            {
                var kvp = pair.Trim().Split('=');
                if (kvp.Length == 2)
                {
                    var key = kvp[0].Trim();
                    var value = kvp[1].Trim('"');
                    parts[key] = value;
                }
            }

            if (!parts.ContainsKey("algorithm") || !parts.ContainsKey("signature") ||
                !parts.ContainsKey("nonce") || !parts.ContainsKey("timestamp"))
            {
                return null;
            }

            var timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(parts["timestamp"]));

            // Parse additional headers if present
            var headers = new Dictionary<string, string>();
            if (parts.ContainsKey("headers"))
            {
                var headerList = parts["headers"].Split(';');
                foreach (var header in headerList)
                {
                    var headerParts = header.Split(':');
                    if (headerParts.Length == 2)
                    {
                        headers[headerParts[0]] = headerParts[1];
                    }
                }
            }

            return new RequestSignature(
                parts["algorithm"],
                parts["signature"],
                parts["nonce"],
                timestamp,
                headers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing signature header");
            return null;
        }
    }
}