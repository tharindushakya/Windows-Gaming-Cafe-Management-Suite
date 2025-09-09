using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;
using System;

namespace GamingCafe.API.Middleware;

public class AuthRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public AuthRateLimitMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        string ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // endpoints we protect
        if (path.StartsWith("/api/auth/login") || path.StartsWith("/api/auth/forgot-password") || path.StartsWith("/api/auth/verify-email") || path.StartsWith("/api/auth/reset-password"))
        {
            var window = TimeSpan.FromMinutes(1);
            int limit = 10; // default for auth-sensitive

            if (path.StartsWith("/api/auth/login"))
                limit = 5;

            var key = $"ratelimit:{path}:{ip}";
            var entryObj = _cache.GetOrCreate(key, ctx =>
            {
                ctx.AbsoluteExpirationRelativeToNow = window;
                return new RateLimitCounter { Count = 0, ResetAt = DateTime.UtcNow.Add(window) };
            });

            var entry = entryObj as RateLimitCounter ?? new RateLimitCounter { Count = 0, ResetAt = DateTime.UtcNow.Add(window) };

            if (entry.Count >= limit)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                var retryAfter = Math.Max(0, (int)entry.ResetAt.Subtract(DateTime.UtcNow).TotalSeconds);
                context.Response.Headers["Retry-After"] = retryAfter.ToString();
                await context.Response.WriteAsync("Too many requests");
                return;
            }

            entry.Count++;
            _cache.Set(key, entry, entry.ResetAt - DateTime.UtcNow);
        }

        await _next(context);
    }

    private class RateLimitCounter
    {
        public int Count { get; set; }
        public DateTime ResetAt { get; set; }
    }
}
