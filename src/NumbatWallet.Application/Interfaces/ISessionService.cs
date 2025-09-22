using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for managing distributed user sessions
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Creates a new session
    /// </summary>
    Task<string> CreateSessionAsync(SessionData sessionData, TimeSpan? expiration = null);

    /// <summary>
    /// Gets session data by session ID
    /// </summary>
    Task<SessionData?> GetSessionAsync(string sessionId);

    /// <summary>
    /// Updates an existing session
    /// </summary>
    Task<bool> UpdateSessionAsync(string sessionId, SessionData sessionData);

    /// <summary>
    /// Deletes a session
    /// </summary>
    Task<bool> DeleteSessionAsync(string sessionId);

    /// <summary>
    /// Extends session expiration
    /// </summary>
    Task<bool> ExtendSessionAsync(string sessionId, TimeSpan extension);

    /// <summary>
    /// Validates if a session is still valid
    /// </summary>
    Task<bool> ValidateSessionAsync(string sessionId);

    /// <summary>
    /// Gets all sessions for a user
    /// </summary>
    Task<IEnumerable<SessionData>> GetUserSessionsAsync(string userId);

    /// <summary>
    /// Revokes all sessions for a user
    /// </summary>
    Task<bool> RevokeUserSessionsAsync(string userId);

    /// <summary>
    /// Gets device session information
    /// </summary>
    Task<DeviceSession?> GetDeviceSessionAsync(string deviceId);

    /// <summary>
    /// Registers a new device
    /// </summary>
    Task<bool> RegisterDeviceAsync(string deviceId, DeviceSession deviceSession);

    /// <summary>
    /// Unregisters a device
    /// </summary>
    Task<bool> UnregisterDeviceAsync(string deviceId);
}