using System.Diagnostics.Metrics;

namespace GamingCafe.API.Middleware;

public static class RateLimitingMetrics
{
    private static readonly Meter _meter = new("GamingCafe.RateLimiter", "1.0");
    public static Counter<long> AllowedCounter { get; } = _meter.CreateCounter<long>("rate_limit_allowed", description: "Number of requests allowed by rate limiter");
    public static Counter<long> RejectedCounter { get; } = _meter.CreateCounter<long>("rate_limit_rejected", description: "Number of requests rejected by rate limiter");

    // Simple atomic gauges for Prometheus-style scraping
    private static long _allowed = 0;
    private static long _rejected = 0;

    public static void IncrementAllowed(long value = 1)
    {
        AllowedCounter.Add(value);
        System.Threading.Interlocked.Add(ref _allowed, value);
    }

    public static void IncrementRejected(long value = 1)
    {
        RejectedCounter.Add(value);
        System.Threading.Interlocked.Add(ref _rejected, value);
    }

    public static long GetAllowed() => System.Threading.Volatile.Read(ref _allowed);
    public static long GetRejected() => System.Threading.Volatile.Read(ref _rejected);
}
