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

        public QueuedHostedService(IBackgroundTaskQueue taskQueue, ILogger<QueuedHostedService> logger)
        {
            _taskQueue = taskQueue;
            _logger = logger;
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
                            attempt++;
                            _logger.LogWarning(ex, "Background task failed on attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
                            if (attempt <= maxRetries)
                            {
                                // simple exponential backoff
                                var computed = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                                var backoff = computed < TimeSpan.FromMinutes(5) ? computed : TimeSpan.FromMinutes(5);
                                await Task.Delay(backoff, stoppingToken);
                            }
                        }
                    }

                    if (!success && lastEx != null)
                    {
                        _logger.LogError(lastEx, "Background task failed after {Attempts} attempts", attempt);
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
