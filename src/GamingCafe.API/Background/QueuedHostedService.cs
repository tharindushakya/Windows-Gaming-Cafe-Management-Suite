using GamingCafe.Core.Interfaces.Background;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace GamingCafe.API.Background
{
    public class QueuedHostedService : BackgroundService
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ILogger<QueuedHostedService> _logger;

        private readonly BackgroundTaskQueue? _impl;

        public QueuedHostedService(IBackgroundTaskQueue taskQueue, ILogger<QueuedHostedService> logger)
        {
            _taskQueue = taskQueue;
            _logger = logger;

            // try to cast to concrete for metrics hooks
            _impl = taskQueue as BackgroundTaskQueue;
        }

        private static readonly System.Threading.ThreadLocal<System.Random> _random = new(() => new System.Random());

        private static bool IsTransient(Exception ex)
        {
            // Basic heuristic: treat network/database timeouts and transient DB exceptions as retryable.
            var typeName = ex.GetType().Name;

            if (typeName.Contains("Timeout") || typeName.Contains("Transient") || typeName.Contains("HttpRequestException"))
                return true;

            // Npgsql specific
            if (typeName.Contains("Postgres") || typeName.Contains("NpgsqlException"))
                return true;

            // SqlException typically indicates DB errors; consider them transient for retrying in many cases
            if (typeName.Contains("SqlException"))
                return true;

            // Otherwise conservative: don't retry on unknown exceptions
            return false;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background task processing is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var (workItem, maxRetries, scheduled) = await _taskQueue.DequeueAsync(stoppingToken);

                    // If scheduled for the future, wait (respect cancellation)
                    if (scheduled.HasValue && scheduled.Value > DateTimeOffset.UtcNow)
                    {
                        var delay = scheduled.Value - DateTimeOffset.UtcNow;
                        _logger.LogDebug("Delaying background work by {Delay} until scheduled time {Scheduled}", delay, scheduled.Value);
                        await Task.Delay(delay, stoppingToken);
                    }

                    var attempt = 0;
                    var success = false;
                    Exception? lastEx = null;

                    while (attempt <= maxRetries && !stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            await workItem(stoppingToken);
                            success = true;
                            break;
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("Background task cancelled due to shutdown");
                            break;
                        }
                        catch (Exception ex)
                        {
                            lastEx = ex;

                            // Check if error looks transient; if not, don't retry
                            if (!IsTransient(ex))
                            {
                                _logger.LogError(ex, "Background task failed with non-transient error, will not retry");
                                break;
                            }

                            attempt++;
                            _logger.LogWarning(ex, "Background task failed on attempt {Attempt}/{MaxRetries}", attempt, maxRetries);

                            if (attempt <= maxRetries)
                            {
                                // jittered exponential backoff: base 2^attempt seconds with +-25% jitter, capped
                                var baseSeconds = Math.Pow(2, attempt);
                                var maxBackoff = TimeSpan.FromMinutes(5);
                                var baseBackoff = TimeSpan.FromSeconds(baseSeconds) < maxBackoff ? TimeSpan.FromSeconds(baseSeconds) : maxBackoff;
                                var jitter = TimeSpan.FromMilliseconds((new System.Random()).NextDouble() * baseBackoff.TotalMilliseconds * 0.5 - baseBackoff.TotalMilliseconds * 0.25);
                                var backoff = baseBackoff + jitter;
                                if (backoff < TimeSpan.FromMilliseconds(100)) backoff = TimeSpan.FromMilliseconds(100);

                                await Task.Delay(backoff, stoppingToken);
                            }
                        }
                    }

                    if (!success && lastEx != null)
                    {
                        _logger.LogError(lastEx, "Background task failed after {Attempts} attempts", attempt);
                        _impl?.IncrementFailure();
                    }
                }
                catch (OperationCanceledException)
                {
                    // shutdown requested
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while dequeuing or processing background work item");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }

            _logger.LogInformation("Background task processing is stopping.");
        }
    }
}
