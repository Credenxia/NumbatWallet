using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for managing time-limited unmask sessions.
/// Provides temporary access to unredacted values.
/// </summary>
public interface IUnmaskCacheService
{
    /// <summary>
    /// Creates a new unmask session for a user
    /// </summary>
    /// <param name="userId">The user requesting unmask access</param>
    /// <param name="entityIds">The entity IDs to unmask</param>
    /// <param name="fields">The fields to unmask</param>
    /// <param name="reason">The reason for unmasking (required for sensitive data)</param>
    /// <param name="ttlSeconds">Time to live in seconds</param>
    /// <returns>The created unmask session</returns>
    Task<UnmaskSession> CreateSessionAsync(
        string userId,
        string[] entityIds,
        string[] fields,
        string? reason,
        int ttlSeconds);

    /// <summary>
    /// Gets an unmasked value from a session
    /// </summary>
    /// <typeparam name="T">The type of value</typeparam>
    /// <param name="sessionId">The unmask session ID</param>
    /// <param name="entityId">The entity ID</param>
    /// <param name="fieldName">The field name</param>
    /// <returns>The unmasked value, or null if not in session</returns>
    Task<T?> GetUnmaskedValueAsync<T>(
        string sessionId,
        string entityId,
        string fieldName);

    /// <summary>
    /// Gets an active session for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>The active session, or null if none exists</returns>
    Task<UnmaskSession?> GetActiveSessionAsync(string userId);

    /// <summary>
    /// Validates an unmask token
    /// </summary>
    /// <param name="token">The unmask token</param>
    /// <returns>The session if valid, null otherwise</returns>
    Task<UnmaskSession?> ValidateTokenAsync(string token);

    /// <summary>
    /// Revokes an unmask session
    /// </summary>
    /// <param name="sessionId">The session ID to revoke</param>
    Task RevokeSessionAsync(string sessionId);

    /// <summary>
    /// Revokes all sessions for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    Task RevokeAllSessionsForUserAsync(string userId);

    /// <summary>
    /// Extends a session's TTL
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="additionalSeconds">Additional seconds to add</param>
    Task ExtendSessionAsync(string sessionId, int additionalSeconds);

    /// <summary>
    /// Gets session statistics for monitoring
    /// </summary>
    /// <param name="userId">Optional user ID filter</param>
    /// <returns>Session statistics</returns>
    Task<UnmaskSessionStats> GetSessionStatsAsync(string? userId = null);
}

/// <summary>
/// Represents an unmask session
/// </summary>
public class UnmaskSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public Dictionary<string, Dictionary<string, object>> UnmaskedValues { get; set; } = new();
    public string[] EntityIds { get; set; } = Array.Empty<string>();
    public string[] Fields { get; set; } = Array.Empty<string>();
    public string? UnmaskReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public int AccessCount { get; set; }
    public bool IsActive => DateTimeOffset.UtcNow < ExpiresAt;
}

/// <summary>
/// Statistics for unmask sessions
/// </summary>
public class UnmaskSessionStats
{
    public int ActiveSessions { get; set; }
    public int TotalSessionsToday { get; set; }
    public int TotalUnmaskOperations { get; set; }
    public Dictionary<DataClassification, int> UnmasksByClassification { get; set; } = new();
    public List<string> TopUnmaskedFields { get; set; } = new();
}