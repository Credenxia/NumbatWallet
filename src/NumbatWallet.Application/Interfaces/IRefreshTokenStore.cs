namespace NumbatWallet.Application.Interfaces;

/// <summary>The data persisted against a refresh token.</summary>
public sealed record RefreshTokenData(string UserId, IReadOnlyList<string> Roles);

/// <summary>
/// Stores issued refresh tokens so they can be validated and rotated on refresh and
/// revoked on logout. Backed by a distributed cache (Redis) in production so it survives
/// restarts and works across multiple instances.
/// </summary>
public interface IRefreshTokenStore
{
    /// <summary>Persist a refresh token (with the user's roles) until <paramref name="expiryUtc"/>.</summary>
    void Store(string refreshToken, string userId, IReadOnlyList<string> roles, DateTime expiryUtc);

    /// <summary>Return the associated data if the token exists and is unexpired; otherwise null.</summary>
    RefreshTokenData? Get(string refreshToken);

    /// <summary>Remove a refresh token (rotation / logout).</summary>
    void Revoke(string refreshToken);
}
