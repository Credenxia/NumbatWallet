using System.Collections.Concurrent;

namespace NumbatWallet.Application.Services;

/// <summary>
/// POA Implementation: In-memory token blacklist service
/// In production, this would use Redis or distributed cache
/// </summary>
public interface ITokenBlacklistService
{
    void BlacklistToken(string token);
    bool IsTokenBlacklisted(string token);
    void Clear(); // For testing
}

public class TokenBlacklistService : ITokenBlacklistService
{
    private static readonly ConcurrentDictionary<string, DateTime> _blacklistedTokens = new();

    public void BlacklistToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);

        // Store token with expiry time (1 hour from now, matching JWT expiry)
        _blacklistedTokens.TryAdd(token, DateTime.UtcNow.AddHours(1));

        // Clean up expired tokens periodically
        CleanupExpiredTokens();
    }

    public bool IsTokenBlacklisted(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        return _blacklistedTokens.ContainsKey(token);
    }

    public void Clear()
    {
        _blacklistedTokens.Clear();
    }

    private static void CleanupExpiredTokens()
    {
        var now = DateTime.UtcNow;
        var expiredTokens = _blacklistedTokens
            .Where(kvp => kvp.Value < now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var token in expiredTokens)
        {
            _blacklistedTokens.TryRemove(token, out _);
        }
    }
}
