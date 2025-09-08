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
                    var workItem = await _taskQueue.DequeueAsync(stoppingToken);
                    try
                    {
                        await workItem(stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        // graceful shutdown
                        _logger.LogInformation("Background task cancelled due to shutdown");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred executing background work item");
                    }
                }
                catch (OperationCanceledException)
                {
                    // shutdown requested
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while dequeuing background work item");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }

            _logger.LogInformation("Background task processing is stopping.");
        }
    }
}
