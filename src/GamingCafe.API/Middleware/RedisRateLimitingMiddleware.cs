using System.Text.Json;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;

namespace GamingCafe.API.Middleware;

public class RedisRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDatabase _db;
    private readonly ILogger<RedisRateLimitingMiddleware> _logger;
    private readonly RateLimitingOptions _options;

    public RedisRateLimitingMiddleware(RequestDelegate next, IConnectionMultiplexer multiplexer, ILogger<RedisRateLimitingMiddleware> logger, RateLimitingOptions options)
    {
        _next = next;
        _db = multiplexer.GetDatabase();
        _logger = logger;
        _options = options ?? new RateLimitingOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"rl:{_options.Prefix}:{ip}";

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var windowStart = now - (_options.WindowSeconds);

        // Use Redis sorted set as sliding window: add current timestamp, remove old, get count
        var tran = _db.CreateTransaction();
        _ = tran.SortedSetAddAsync(key, now.ToString(), now);
        _ = tran.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart - 1);
        _ = tran.KeyExpireAsync(key, TimeSpan.FromSeconds(_options.WindowSeconds + 5));
        var exec = await tran.ExecuteAsync();
        if (!exec)
        {
            // Fallback to allow request through if Redis transaction fails
            await _next(context);
            return;
        }

        var count = await _db.SortedSetLengthAsync(key);
        if (count > _options.Limit)
        {
            _logger.LogWarning("Rate limit exceeded for IP {IP}: {Count}/{Limit}", ip, count, _options.Limit);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = (_options.WindowSeconds).ToString();
            await context.Response.WriteAsync("Too many requests. Please retry later.");
            return;
        }

        await _next(context);
    }
}
