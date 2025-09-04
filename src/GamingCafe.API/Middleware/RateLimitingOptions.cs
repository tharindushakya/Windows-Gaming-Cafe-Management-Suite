namespace GamingCafe.API.Middleware;

public class RateLimitingOptions
{
    public string Prefix { get; set; } = "default";
    public int Limit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60; // sliding window in seconds
}
