using System.Text.Json;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;

namespace GamingCafe.API.Middleware;

public class RedisRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDatabase? _db;
    private readonly ILogger<RedisRateLimitingMiddleware> _logger;
    private readonly RateLimitingOptions _options;
    // In-memory fallback store: key -> list of timestamps
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentQueue<long>> _inMemory = new();

    public RedisRateLimitingMiddleware(RequestDelegate next, IConnectionMultiplexer? multiplexer, ILogger<RedisRateLimitingMiddleware> logger, RateLimitingOptions options)
    {
        _next = next;
        _db = multiplexer?.GetDatabase();
        _logger = logger;
        _options = options ?? new RateLimitingOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.Value ?? "/";

        // Whitelist
        if (_options.Whitelist != null && _options.Whitelist.Contains(ip))
        {
            await _next(context);
            return;
        }

        // Exempt paths
        if (_options.ExemptPaths != null && _options.ExemptPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Determine overrides
        var limit = _options.Limit;
        var windowSeconds = _options.WindowSeconds;
        if (_options.Overrides != null)
        {
            var match = _options.Overrides.FirstOrDefault(kv => path.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase));
            if (!match.Equals(default(KeyValuePair<string, RateLimitOverride>)))
            {
                limit = match.Value.Limit;
                windowSeconds = match.Value.WindowSeconds;
            }
        }

        var key = $"rl:{_options.Prefix}:{ip}";

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var windowStart = now - windowSeconds;

        // Use Redis sorted set as sliding window: add current timestamp, remove old, get count
        long count;

        if (_db != null)
        {
            var tran = _db.CreateTransaction();
            _ = tran.SortedSetAddAsync(key, now.ToString(), now);
            _ = tran.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart - 1);
            _ = tran.KeyExpireAsync(key, TimeSpan.FromSeconds(windowSeconds + 5));
            var exec = await tran.ExecuteAsync();
            if (!exec)
            {
                // Fallback to in-memory if Redis transaction fails
                _logger.LogWarning("Redis transaction failed for key {Key}; falling back to in-memory rate limiting", key);
                count = InMemorySlidingWindow(key, now, windowSeconds);
            }
            else
            {
                count = await _db.SortedSetLengthAsync(key);
            }
        }
        else
        {
            // No Redis - use in-memory sliding window
            count = InMemorySlidingWindow(key, now, windowSeconds);
        }
        if (count > limit)
        {
            _logger.LogWarning("Rate limit exceeded for IP {IP}: {Count}/{Limit}", ip, count, limit);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = windowSeconds.ToString();
            context.Response.ContentType = "application/problem+json";
            var payload = new
            {
                type = "https://httpstatuses.com/429",
                title = "Too Many Requests",
                status = 429,
                detail = "Rate limit exceeded. Please retry later.",
                instance = context.Request.Path.Value,
                retry_after = windowSeconds
            };
            RateLimitingMetrics.IncrementRejected(1);
            await context.Response.WriteAsJsonAsync(payload);
            return;
        }

    RateLimitingMetrics.IncrementAllowed(1);
        await _next(context);
    }

    private long InMemorySlidingWindow(string key, long now, int windowSeconds)
    {
        var queue = _inMemory.GetOrAdd(key, _ => new System.Collections.Concurrent.ConcurrentQueue<long>());

        // Enqueue current timestamp
        queue.Enqueue(now);

        // Dequeue old timestamps
        while (queue.TryPeek(out var ts) && ts < now - windowSeconds)
        {
            queue.TryDequeue(out _);
        }

        return queue.Count;
    }
}
