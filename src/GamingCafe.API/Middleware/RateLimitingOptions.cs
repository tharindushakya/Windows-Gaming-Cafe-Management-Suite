namespace GamingCafe.API.Middleware;

public class RateLimitingOptions
{
    public string Prefix { get; set; } = "default";
    public int Limit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60; // sliding window in seconds
    // Optional list of IPs (or exact strings) to whitelist from limiting (e.g., internal services)
    public string[]? Whitelist { get; set; }

    // Paths (prefixes) exempt from rate limiting (e.g., health, metrics)
    public string[]? ExemptPaths { get; set; }

    // Per-path overrides. Keys are path prefixes (e.g., "/api/auth" ) and value controls the limit/window.
    public Dictionary<string, RateLimitOverride>? Overrides { get; set; }
}

public class RateLimitOverride
{
    public int Limit { get; set; }
    public int WindowSeconds { get; set; }
}
