using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

/// <summary>
/// Service for managing distributed sessions using Redis
/// </summary>
public class DistributedSessionService : ISessionService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedSessionService> _logger;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(1);
    private readonly TimeSpan _slidingExpiration = TimeSpan.FromMinutes(20);

    public DistributedSessionService(
        IDistributedCache cache,
        ILogger<DistributedSessionService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> CreateSessionAsync(SessionData sessionData, TimeSpan? expiration = null)
    {
        var sessionId = GenerateSessionId();
        var key = GetSessionKey(sessionId);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
            SlidingExpiration = _slidingExpiration
        };

        var json = JsonSerializer.Serialize(sessionData);
        await _cache.SetStringAsync(key, json, options);

        _logger.LogInformation("Created session {SessionId} for user {UserId}", sessionId, sessionData.UserId);

        return sessionId;
    }

    public async Task<SessionData?> GetSessionAsync(string sessionId)
    {
        var key = GetSessionKey(sessionId);
        var json = await _cache.GetStringAsync(key);

        if (string.IsNullOrEmpty(json))
        {
            _logger.LogDebug("Session {SessionId} not found", sessionId);
            return null;
        }

        try
        {
            var sessionData = JsonSerializer.Deserialize<SessionData>(json);

            // Update last accessed time
            if (sessionData != null)
            {
                sessionData.LastAccessedAt = DateTimeOffset.UtcNow;
                await UpdateSessionAsync(sessionId, sessionData);
            }

            return sessionData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<bool> UpdateSessionAsync(string sessionId, SessionData sessionData)
    {
        var key = GetSessionKey(sessionId);
        var existingJson = await _cache.GetStringAsync(key);

        if (string.IsNullOrEmpty(existingJson))
        {
            _logger.LogWarning("Attempted to update non-existent session {SessionId}", sessionId);
            return false;
        }

        sessionData.LastAccessedAt = DateTimeOffset.UtcNow;
        var json = JsonSerializer.Serialize(sessionData);

        // Refresh with sliding expiration
        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = _slidingExpiration
        };

        await _cache.SetStringAsync(key, json, options);

        _logger.LogDebug("Updated session {SessionId}", sessionId);
        return true;
    }

    public async Task<bool> DeleteSessionAsync(string sessionId)
    {
        var key = GetSessionKey(sessionId);
        await _cache.RemoveAsync(key);

        _logger.LogInformation("Deleted session {SessionId}", sessionId);
        return true;
    }

    public async Task<bool> ExtendSessionAsync(string sessionId, TimeSpan extension)
    {
        var sessionData = await GetSessionAsync(sessionId);
        if (sessionData == null)
        {
            return false;
        }

        var key = GetSessionKey(sessionId);
        var json = JsonSerializer.Serialize(sessionData);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = extension,
            SlidingExpiration = _slidingExpiration
        };

        await _cache.SetStringAsync(key, json, options);

        _logger.LogDebug("Extended session {SessionId} by {Extension}", sessionId, extension);
        return true;
    }

    public async Task<bool> ValidateSessionAsync(string sessionId)
    {
        var sessionData = await GetSessionAsync(sessionId);
        if (sessionData == null)
        {
            return false;
        }

        // Check if session is expired
        if (sessionData.ExpiresAt.HasValue && sessionData.ExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Session {SessionId} has expired", sessionId);
            await DeleteSessionAsync(sessionId);
            return false;
        }

        // Check if session is still valid (not revoked)
        if (sessionData.IsRevoked)
        {
            _logger.LogWarning("Session {SessionId} has been revoked", sessionId);
            return false;
        }

        return true;
    }

    public async Task<IEnumerable<SessionData>> GetUserSessionsAsync(string userId)
    {
        // Note: This requires maintaining a separate index of user sessions
        // For production, consider using Redis Sets or Sorted Sets
        var sessions = new List<SessionData>();
        var pattern = $"session:user:{userId}:*";

        // This is a simplified implementation
        // In production, use Redis SCAN command or maintain proper indexes
        _logger.LogDebug("Getting sessions for user {UserId}", userId);

        return sessions;
    }

    public async Task<bool> RevokeUserSessionsAsync(string userId)
    {
        var sessions = await GetUserSessionsAsync(userId);
        var count = 0;

        foreach (var session in sessions)
        {
            if (session.SessionId != null)
            {
                await DeleteSessionAsync(session.SessionId);
                count++;
            }
        }

        _logger.LogInformation("Revoked {Count} sessions for user {UserId}", count, userId);
        return count > 0;
    }

    public async Task<DeviceSession?> GetDeviceSessionAsync(string deviceId)
    {
        var key = $"device:{deviceId}";
        var json = await _cache.GetStringAsync(key);

        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<DeviceSession>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing device session {DeviceId}", deviceId);
            return null;
        }
    }

    public async Task<bool> RegisterDeviceAsync(string deviceId, DeviceSession deviceSession)
    {
        var key = $"device:{deviceId}";
        var json = JsonSerializer.Serialize(deviceSession);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(90) // Device sessions last longer
        };

        await _cache.SetStringAsync(key, json, options);

        _logger.LogInformation("Registered device {DeviceId} for user {UserId}",
            deviceId, deviceSession.UserId);

        return true;
    }

    public async Task<bool> UnregisterDeviceAsync(string deviceId)
    {
        var key = $"device:{deviceId}";
        await _cache.RemoveAsync(key);

        _logger.LogInformation("Unregistered device {DeviceId}", deviceId);
        return true;
    }

    private string GenerateSessionId()
    {
        return $"{Guid.NewGuid():N}";
    }

    private string GetSessionKey(string sessionId)
    {
        return $"session:{sessionId}";
    }
}
