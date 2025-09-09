using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace GamingCafe.Core;

public static class Observability
{
    // ActivitySource for tracing application-specific operations
    public static readonly ActivitySource ActivitySource = new("GamingCafe.App", "1.0.0");

    // Meter for metrics
    public static readonly Meter AppMeter = new("GamingCafe.Metrics", "1.0.0");

    // Common instruments
    public static readonly Counter<long> WalletUpdateCounter = AppMeter.CreateCounter<long>("wallet_updates_total", description: "Total wallet update attempts");
    public static readonly Counter<long> WalletUpdateSuccessCounter = AppMeter.CreateCounter<long>("wallet_updates_success_total", description: "Successful wallet updates");
    public static readonly Histogram<double> WalletUpdateDuration = AppMeter.CreateHistogram<double>("wallet_update_duration_ms", unit: "ms", description: "Duration of wallet update operations");
    // Count of detected refresh-token reuse events (possible theft)
    public static readonly Counter<long> RefreshTokenReuseCounter = AppMeter.CreateCounter<long>("refresh_token_reuse_detected_total", description: "Number of times refresh token reuse was detected and handled");
}
