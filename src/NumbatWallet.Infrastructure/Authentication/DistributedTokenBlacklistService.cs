using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Services;

namespace NumbatWallet.Infrastructure.Authentication;

/// <summary>
/// Distributed-cache (Redis) backed token blacklist. Replaces the previous static
/// ConcurrentDictionary, which made logout/revocation per-process and reset on restart.
/// Entries auto-expire after the access-token lifetime so the blacklist never grows unbounded.
///
/// Cache operations are wrapped defensively: this service runs on the authentication hot path
/// (every authenticated request checks the blacklist), so a cache outage must NOT take the whole
/// API down. Reads fail OPEN (treat as not-blacklisted) with a warning — a transient cache outage
/// degrades the revocation window (bounded by the short token lifetime) rather than denying all
/// traffic. Writes are best-effort.
/// </summary>
public sealed class DistributedTokenBlacklistService(
    IDistributedCache cache,
    ILogger<DistributedTokenBlacklistService> logger) : ITokenBlacklistService
{
    private const string Prefix = "blacklist-token:";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public void BlacklistToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        try
        {
            cache.SetString(
                Prefix + token,
                "1",
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TokenLifetime });
        }
        catch (Exception ex)
        {
            // Best-effort: if we cannot persist the blacklist entry the token simply expires naturally.
            logger.LogError(ex, "Failed to blacklist token in distributed cache");
        }
    }

    public bool IsTokenBlacklisted(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            return cache.GetString(Prefix + token) != null;
        }
        catch (Exception ex)
        {
            // Fail open: a cache outage must not deny all authenticated requests.
            logger.LogWarning(ex, "Token blacklist check failed (cache unavailable); allowing request");
            return false;
        }
    }

    public void Clear()
    {
        // No-op: a distributed cache has no cheap "clear all" and entries auto-expire.
        // Tests that need isolation should use unique tokens or a dedicated cache instance.
    }
}
