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

    private readonly RateLimitingOptions _options;

    // Default values used when options are not provided
    private readonly int _requestsPerWindow;
    private readonly TimeSpan _window;

    public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<RateLimitingMiddleware> logger, RateLimitingOptions? options = null)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _options = options ?? new RateLimitingOptions();
        _requestsPerWindow = _options.Limit;
        _window = TimeSpan.FromSeconds(_options.WindowSeconds);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.Value ?? "/";

        // Whitelist check
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

        // Per-path overrides
        var requestsPerWindow = _requestsPerWindow;
        var window = _window;
        if (_options.Overrides != null)
        {
            var matched = _options.Overrides.FirstOrDefault(kv => path.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase));
            if (!matched.Equals(default(KeyValuePair<string, RateLimitOverride>)))
            {
                requestsPerWindow = matched.Value.Limit;
                window = TimeSpan.FromSeconds(matched.Value.WindowSeconds);
            }
        }

        var cacheKey = $"rl_{_options.Prefix}_{ip}";
        var entry = _cache.GetOrCreate(cacheKey, e =>
        {
            e.AbsoluteExpirationRelativeToNow = window;
            return new RateLimitEntry { Count = 0, WindowStart = DateTime.UtcNow };
        });

        // Defensive null-check to satisfy nullable analysis and avoid possible cache-null scenarios
        if (entry is null)
        {
            entry = new RateLimitEntry { Count = 0, WindowStart = DateTime.UtcNow };
        }

        if (entry.Count >= requestsPerWindow)
        {
            _logger.LogWarning("Rate limit exceeded for IP {IP}", ip);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = ((int)window.TotalSeconds).ToString();
            context.Response.ContentType = "application/problem+json";
            var payload = new
            {
                type = "https://httpstatuses.com/429",
                title = "Too Many Requests",
                status = 429,
                detail = "Rate limit exceeded. Please retry later.",
                instance = context.Request.Path.Value,
                retry_after = (int)window.TotalSeconds
            };
            RateLimitingMetrics.IncrementRejected(1);
            await context.Response.WriteAsJsonAsync(payload);
            return;
        }

    entry.Count++;
    _cache.Set(cacheKey, entry, DateTimeOffset.UtcNow.Add(window));
    RateLimitingMetrics.IncrementAllowed(1);

        await _next(context);
    }

    private class RateLimitEntry
    {
        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
    }
}
