using StackExchange.Redis;

namespace NumbatWallet.Infrastructure.Caching;

/// <summary>
/// Which IDistributedCache backend was selected at registration time. Registered as a
/// singleton so the host can log the choice at startup (the perf baseline showed an
/// unconfigured deployment silently using a nonexistent Redis — the selection must be
/// visible in the logs). <see cref="Description"/> never contains secrets.
/// </summary>
public sealed record CacheBackendSelection(bool UsesRedis, string Description);

/// <summary>
/// Decides which IDistributedCache backend the application uses and how Redis is
/// configured when selected. Extracted as a pure policy so the selection logic is
/// unit-testable (perf defect 2026-06-12: the deployed Testing environment had no
/// ConnectionStrings__Redis injected, fell through to a base-appsettings
/// "localhost:6379" default, and every Bearer request blocked the full 5000ms
/// StackExchange.Redis syncTimeout on the token-blacklist HMGET).
///
/// Rules:
///  - Redis is registered ONLY when a non-empty ConnectionStrings:Redis (or Aspire's
///    "redis" name) is configured — in ANY environment. Unset/empty falls back to the
///    in-memory distributed cache; there is deliberately no localhost default.
///  - Development always uses the in-memory cache regardless of configuration (the
///    local Aspire Redis connection string sets ssl=true against a non-TLS container
///    and is unreliable for dev — see the AddCaching history).
///  - When Redis IS used, timeouts are bounded (abortConnect=false,
///    connectTimeout=1000ms, syncTimeout=500ms) unless the connection string sets them
///    explicitly, so a dead Redis degrades to sub-second fail-open instead of 5s on
///    every authenticated request.
/// </summary>
public static class CacheRegistrationPolicy
{
    public const int DefaultConnectTimeoutMs = 1000;
    public const int DefaultSyncTimeoutMs = 500;

    /// <summary>
    /// True when the Redis distributed cache should be registered: a non-empty
    /// connection string is configured and the environment is not Development.
    /// </summary>
    public static bool UseRedis(string? environmentName, string? redisConnectionString)
    {
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            return false;
        }

        return !string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses the connection string into StackExchange.Redis options with bounded,
    /// fail-open timeouts. Explicit connectTimeout/syncTimeout values in the connection
    /// string are honoured; abortConnect is always forced to false.
    /// </summary>
    public static ConfigurationOptions BuildRedisOptions(string redisConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(redisConnectionString);

        var options = ConfigurationOptions.Parse(redisConnectionString);

        // Never hard-fail startup or requests because Redis is unreachable.
        options.AbortOnConnectFail = false;

        if (!HasExplicitOption(redisConnectionString, "connectTimeout"))
        {
            options.ConnectTimeout = DefaultConnectTimeoutMs;
        }

        if (!HasExplicitOption(redisConnectionString, "syncTimeout"))
        {
            options.SyncTimeout = DefaultSyncTimeoutMs;
        }

        return options;
    }

    /// <summary>
    /// Builds the loggable selection record for the given environment + connection
    /// string. Only endpoints are included — never the raw connection string (it may
    /// carry a password).
    /// </summary>
    public static CacheBackendSelection DescribeSelection(string? environmentName, string? redisConnectionString)
    {
        if (UseRedis(environmentName, redisConnectionString))
        {
            var endpoints = string.Join(",", BuildRedisOptions(redisConnectionString!).EndPoints);
            return new CacheBackendSelection(
                true,
                $"Redis distributed cache ({endpoints}; bounded timeouts, fail-open)");
        }

        var reason = string.IsNullOrWhiteSpace(redisConnectionString)
            ? "no Redis connection string configured"
            : $"environment '{environmentName}' forces in-memory";
        return new CacheBackendSelection(false, $"in-memory distributed cache ({reason})");
    }

    private static bool HasExplicitOption(string connectionString, string optionName)
    {
        foreach (var part in connectionString.Split(','))
        {
            if (part.TrimStart().StartsWith(optionName + "=", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
