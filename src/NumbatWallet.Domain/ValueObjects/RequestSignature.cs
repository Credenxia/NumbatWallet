using NumbatWallet.SharedKernel.Guards;

namespace NumbatWallet.Domain.ValueObjects;

/// <summary>
/// Value object representing a cryptographic signature for request validation
/// </summary>
public class RequestSignature : IEquatable<RequestSignature>
{
    public string Algorithm { get; }
    public string Signature { get; }
    public string Nonce { get; }
    public DateTimeOffset Timestamp { get; }
    public Dictionary<string, string> Headers { get; }

    public RequestSignature(
        string algorithm,
        string signature,
        string nonce,
        DateTimeOffset timestamp,
        Dictionary<string, string>? headers = null)
    {
        Guard.AgainstNullOrWhiteSpace(algorithm, nameof(algorithm));
        Guard.AgainstNullOrWhiteSpace(signature, nameof(signature));
        Guard.AgainstNullOrWhiteSpace(nonce, nameof(nonce));

        Algorithm = algorithm.ToUpperInvariant();
        Signature = signature;
        Nonce = nonce;
        Timestamp = timestamp;
        Headers = headers ?? new Dictionary<string, string>();

        ValidateAlgorithm();
        ValidateTimestamp();
    }

    private void ValidateAlgorithm()
    {
        var supportedAlgorithms = new[] { "SHA256", "SHA384", "SHA512", "RSA-SHA256", "RSA-SHA512" };
        if (!supportedAlgorithms.Contains(Algorithm))
        {
            throw new ArgumentException($"Unsupported signature algorithm: {Algorithm}");
        }
    }

    private void ValidateTimestamp()
    {
        var fiveMinutesAgo = DateTimeOffset.UtcNow.AddMinutes(-5);
        var fiveMinutesFromNow = DateTimeOffset.UtcNow.AddMinutes(5);

        if (Timestamp < fiveMinutesAgo || Timestamp > fiveMinutesFromNow)
        {
            throw new ArgumentException("Request timestamp is outside the acceptable window (±5 minutes)");
        }
    }

    public bool IsExpired(TimeSpan? customWindow = null)
    {
        var window = customWindow ?? TimeSpan.FromMinutes(5);
        return Math.Abs((DateTimeOffset.UtcNow - Timestamp).TotalMilliseconds) > window.TotalMilliseconds;
    }

    public string GetCanonicalHeaders()
    {
        if (Headers.Count == 0)
        {
            return string.Empty;
        }

        var sortedHeaders = Headers
            .OrderBy(h => h.Key.ToLowerInvariant())
            .Select(h => $"{h.Key.ToLowerInvariant()}:{h.Value.Trim()}");

        return string.Join("\n", sortedHeaders);
    }

    public string GetSignatureBase(string method, string path, string? body = null)
    {
        var parts = new List<string>
        {
            method.ToUpperInvariant(),
            path,
            GetCanonicalHeaders(),
            Nonce,
            Timestamp.ToUnixTimeSeconds().ToString(),
            body ?? string.Empty
        };

        return string.Join("\n", parts);
    }

    public bool Equals(RequestSignature? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Algorithm == other.Algorithm &&
               Signature == other.Signature &&
               Nonce == other.Nonce &&
               Timestamp == other.Timestamp;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as RequestSignature);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Algorithm, Signature, Nonce, Timestamp);
    }

    public static bool operator ==(RequestSignature? left, RequestSignature? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(RequestSignature? left, RequestSignature? right)
    {
        return !Equals(left, right);
    }
}
