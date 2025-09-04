using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using GamingCafe.Core.Interfaces.Services;

namespace GamingCafe.Core.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CacheService(
        IDistributedCache distributedCache, 
        IConnectionMultiplexer redis, 
        ILogger<CacheService> logger)
    {
        _distributedCache = distributedCache;
        _redis = redis;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _distributedCache.GetStringAsync(key);
            
            if (string.IsNullOrEmpty(value))
                return default;

            return JsonSerializer.Deserialize<T>(value, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key: {Key}", key);
            return default;
        }
    }

    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            return await _distributedCache.GetStringAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached string value for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            
            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                // Default expiration of 1 hour
                options.SetAbsoluteExpiration(TimeSpan.FromHours(1));
            }

            await _distributedCache.SetStringAsync(key, serializedValue, options);
            
            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", 
                key, expiration ?? TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached value for key: {Key}", key);
        }
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiration = null)
    {
        try
        {
            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                options.SetAbsoluteExpiration(TimeSpan.FromHours(1));
            }

            await _distributedCache.SetStringAsync(key, value, options);
            
            _logger.LogDebug("Cached string value for key: {Key} with expiration: {Expiration}", 
                key, expiration ?? TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached string value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _distributedCache.RemoveAsync(key);
            _logger.LogDebug("Removed cached value for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var database = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            
            var keys = server.Keys(pattern: pattern);
            
            if (keys.Any())
            {
                await database.KeyDeleteAsync(keys.ToArray());
                _logger.LogDebug("Removed {Count} cached values matching pattern: {Pattern}", 
                    keys.Count(), pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached values by pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var value = await _distributedCache.GetStringAsync(key);
            return !string.IsNullOrEmpty(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if cached value exists for key: {Key}", key);
            return false;
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        try
        {
            // Try to get from cache first
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cachedValue;
            }

            // Not in cache, get from factory
            _logger.LogDebug("Cache miss for key: {Key}, executing factory", key);
            var value = await factory();
            
            // Cache the result
            await SetAsync(key, value, expiration);
            
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSetAsync for key: {Key}", key);
            // Fallback to factory if cache fails
            return await factory();
        }
    }

    public async Task ClearAllAsync()
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            await server.FlushDatabaseAsync();
            
            _logger.LogInformation("Cleared all cached values");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all cached values");
        }
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string pattern = "*")
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);
            
            return keys.Select(k => k.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting keys with pattern: {Pattern}", pattern);
            return Enumerable.Empty<string>();
        }
    }

    // Gaming Cafe specific caching methods
    public async Task<T?> GetUserCacheAsync<T>(int userId, string cacheType)
    {
        var key = $"user:{userId}:{cacheType}";
        return await GetAsync<T>(key);
    }

    public async Task SetUserCacheAsync<T>(int userId, string cacheType, T value, TimeSpan? expiration = null)
    {
        var key = $"user:{userId}:{cacheType}";
        await SetAsync(key, value, expiration);
    }

    public async Task RemoveUserCacheAsync(int userId, string? cacheType = null)
    {
        if (string.IsNullOrEmpty(cacheType))
        {
            // Remove all cache for user
            await RemoveByPatternAsync($"user:{userId}:*");
        }
        else
        {
            // Remove specific cache type for user
            var key = $"user:{userId}:{cacheType}";
            await RemoveAsync(key);
        }
    }

    public async Task<T?> GetStationCacheAsync<T>(int stationId, string cacheType)
    {
        var key = $"station:{stationId}:{cacheType}";
        return await GetAsync<T>(key);
    }

    public async Task SetStationCacheAsync<T>(int stationId, string cacheType, T value, TimeSpan? expiration = null)
    {
        var key = $"station:{stationId}:{cacheType}";
        await SetAsync(key, value, expiration);
    }

    public async Task RemoveStationCacheAsync(int stationId, string? cacheType = null)
    {
        if (string.IsNullOrEmpty(cacheType))
        {
            await RemoveByPatternAsync($"station:{stationId}:*");
        }
        else
        {
            var key = $"station:{stationId}:{cacheType}";
            await RemoveAsync(key);
        }
    }

    public async Task<T?> GetSessionCacheAsync<T>(int sessionId, string cacheType)
    {
        var key = $"session:{sessionId}:{cacheType}";
        return await GetAsync<T>(key);
    }

    public async Task SetSessionCacheAsync<T>(int sessionId, string cacheType, T value, TimeSpan? expiration = null)
    {
        var key = $"session:{sessionId}:{cacheType}";
        await SetAsync(key, value, expiration ?? TimeSpan.FromMinutes(30)); // Sessions are more volatile
    }

    public async Task RemoveSessionCacheAsync(int sessionId, string? cacheType = null)
    {
        if (string.IsNullOrEmpty(cacheType))
        {
            await RemoveByPatternAsync($"session:{sessionId}:*");
        }
        else
        {
            var key = $"session:{sessionId}:{cacheType}";
            await RemoveAsync(key);
        }
    }

    // Statistics and reporting cache
    public async Task<T?> GetReportCacheAsync<T>(string reportType, DateTime date)
    {
        var key = $"report:{reportType}:{date:yyyyMMdd}";
        return await GetAsync<T>(key);
    }

    public async Task SetReportCacheAsync<T>(string reportType, DateTime date, T value)
    {
        var key = $"report:{reportType}:{date:yyyyMMdd}";
        // Reports can be cached for longer periods
        await SetAsync(key, value, TimeSpan.FromHours(6));
    }

    // Invalidate related caches when entities change
    public async Task InvalidateUserRelatedCacheAsync(int userId)
    {
        await Task.WhenAll(
            RemoveUserCacheAsync(userId),
            RemoveByPatternAsync($"*:user:{userId}"),
            RemoveByPatternAsync("statistics:*"),
            RemoveByPatternAsync("report:*")
        );
    }

    public async Task InvalidateStationRelatedCacheAsync(int stationId)
    {
        await Task.WhenAll(
            RemoveStationCacheAsync(stationId),
            RemoveByPatternAsync($"*:station:{stationId}"),
            RemoveByPatternAsync("stations:*"),
            RemoveByPatternAsync("statistics:*")
        );
    }

    public async Task InvalidateSessionRelatedCacheAsync(int sessionId, int userId, int stationId)
    {
        await Task.WhenAll(
            RemoveSessionCacheAsync(sessionId),
            RemoveUserCacheAsync(userId, "sessions"),
            RemoveStationCacheAsync(stationId, "sessions"),
            RemoveByPatternAsync("statistics:*"),
            RemoveByPatternAsync("report:*")
        );
    }
}
