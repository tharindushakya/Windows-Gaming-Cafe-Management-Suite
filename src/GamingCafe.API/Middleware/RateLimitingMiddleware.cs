using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace GamingCafe.API.Middleware;

/// <summary>
/// Simple IP-based rate limiting middleware (sliding window) for basic protection.
/// Not a production-grade rate limiter; replace with a distributed limiter for multi-node deployments.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    // Limit per window
    private readonly int _requestsPerWindow = 100; // default
    private readonly TimeSpan _window = TimeSpan.FromMinutes(1);

    public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var cacheKey = $"rl_{ip}";
        var entry = _cache.GetOrCreate(cacheKey, e =>
        {
            e.AbsoluteExpirationRelativeToNow = _window;
            return new RateLimitEntry { Count = 0, WindowStart = DateTime.UtcNow };
        });

        if (entry.Count >= _requestsPerWindow)
        {
            _logger.LogWarning("Rate limit exceeded for IP {IP}", ip);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = ((int)_window.TotalSeconds).ToString();
            await context.Response.WriteAsync("Too many requests. Please retry later.");
            return;
        }

        entry.Count++;
        _cache.Set(cacheKey, entry, DateTimeOffset.UtcNow.Add(_window));

        await _next(context);
    }

    private class RateLimitEntry
    {
        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
    }
}
