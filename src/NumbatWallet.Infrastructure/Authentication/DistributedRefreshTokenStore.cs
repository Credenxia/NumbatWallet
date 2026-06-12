using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Authentication;

/// <summary>
/// Distributed-cache (Redis) backed refresh-token store. Replaces the previous static
/// in-memory dictionary, which lost all tokens on restart and did not work across instances.
/// The user's roles are persisted alongside the token so they survive refresh-token rotation.
///
/// Cache operations are wrapped defensively so a cache outage degrades gracefully (the user must
/// re-authenticate) rather than throwing 500s. Reads fail SAFE (return null → token treated as
/// invalid); writes are best-effort.
/// </summary>
public sealed class DistributedRefreshTokenStore(
    IDistributedCache cache,
    ILogger<DistributedRefreshTokenStore> logger) : IRefreshTokenStore
{
    private const string Prefix = "refresh-token:";

    private sealed record Entry(string UserId, string[] Roles);

    public void Store(string refreshToken, string userId, IReadOnlyList<string> roles, DateTime expiryUtc)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var ttl = expiryUtc - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        try
        {
            var payload = JsonSerializer.Serialize(new Entry(userId, roles.ToArray()));
            cache.SetString(
                Prefix + refreshToken,
                payload,
                new DistributedCacheEntryOptions { AbsoluteExpiration = new DateTimeOffset(expiryUtc, TimeSpan.Zero) });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist refresh token in distributed cache");
        }
    }

    public RefreshTokenData? Get(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        string? payload;
        try
        {
            payload = cache.GetString(Prefix + refreshToken);
        }
        catch (Exception ex)
        {
            // Fail safe: treat as invalid so the caller is asked to re-authenticate.
            logger.LogWarning(ex, "Refresh token lookup failed (cache unavailable)");
            return null;
        }

        if (string.IsNullOrEmpty(payload))
        {
            return null;
        }

        try
        {
            var entry = JsonSerializer.Deserialize<Entry>(payload);
            return entry is null ? null : new RefreshTokenData(entry.UserId, entry.Roles);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Revoke(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        try
        {
            cache.Remove(Prefix + refreshToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke refresh token in distributed cache");
        }
    }
}
