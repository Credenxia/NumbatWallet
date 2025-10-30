using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace NumbatWallet.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IDistributedCache _distributedCache,
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cached value for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value, _jsonOptions);
            var expiry = expiration ?? TimeSpan.FromHours(1);

            await _database.StringSetAsync(key, serialized, expiry);
            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
            _logger.LogDebug("Removed cached value for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key: {Key}", key);
        }
    }

    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var value = await factory();
        if (value != null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints.First());

            var keys = server.Keys(pattern: $"{prefix}*").ToArray();

            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
                _logger.LogDebug("Removed {Count} cached values with prefix: {Prefix}", keys.Length, prefix);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached values with prefix: {Prefix}", prefix);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }

    public string GenerateCacheKey(params string[] segments)
    {
        return string.Join(":", segments.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    public string GenerateTenantCacheKey(string tenantId, params string[] segments)
    {
        var allSegments = new[] { "tenant", tenantId }.Concat(segments).ToArray();
        return GenerateCacheKey(allSegments);
    }

    // Additional Redis-specific methods
    public async Task<bool> SetIfNotExistsAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value, _jsonOptions);
            var expiry = expiration ?? TimeSpan.FromHours(1);

            var result = await _database.StringSetAsync(key, serialized, expiry, When.NotExists);
            _logger.LogDebug("SetIfNotExists for key: {Key}, Result: {Result}", key, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SetIfNotExists for key: {Key}", key);
            return false;
        }
    }

    public async Task<long> IncrementAsync(
        string key,
        long value = 1,
        TimeSpan? expiration = null)
    {
        try
        {
            var result = await _database.StringIncrementAsync(key, value);

            if (expiration.HasValue)
            {
                await _database.KeyExpireAsync(key, expiration.Value);
            }

            _logger.LogDebug("Incremented key: {Key} by {Value}, New value: {Result}", key, value, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing key: {Key}", key);
            return 0;
        }
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, T?>();

        try
        {
            var keyArray = keys.ToArray();
            var redisKeys = keyArray.Select(k => (RedisKey)k).ToArray();
            var values = await _database.StringGetAsync(redisKeys);
            for (int i = 0; i < keyArray.Length; i++)
            {
                if (!values[i].IsNullOrEmpty)
                {
                    result[keyArray[i]] = JsonSerializer.Deserialize<T>(values[i]!, _jsonOptions);
                }
                else
                {
                    result[keyArray[i]] = default;
                }
            }

            _logger.LogDebug("Retrieved {Count} cached values", values.Count(v => !v.IsNullOrEmpty));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving multiple cached values");
        }

        return result;
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                if (!server.IsReplica)
                {
                    await server.FlushDatabaseAsync();
                    _logger.LogInformation("Cleared all cache entries from Redis");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing Redis cache");
        }
    }

    public async Task<bool> ClearAsync(string? cacheType, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(cacheType) || cacheType?.ToLowerInvariant() == "all")
            {
                await ClearAsync(cancellationToken);
                return true;
            }

            // Clear cache by prefix/pattern for specific cache type
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                if (!server.IsReplica)
                {
                    var pattern = $"{cacheType!.ToLowerInvariant()}:*";
                    var keys = server.Keys(pattern: pattern);

                    foreach (var key in keys)
                    {
                        await _database.KeyDeleteAsync(key);
                    }

                    _logger.LogInformation("Cleared cache entries for type: {CacheType}", cacheType);
                    return true;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache for type: {CacheType}", cacheType);
            return false;
        }
    }
}
