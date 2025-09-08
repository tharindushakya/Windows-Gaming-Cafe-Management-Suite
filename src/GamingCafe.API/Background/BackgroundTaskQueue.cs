using GamingCafe.Core.Interfaces.Background;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GamingCafe.API.Background
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<(Func<CancellationToken, Task> Work, int MaxRetries, DateTimeOffset? Scheduled)> _high;
        private readonly Channel<(Func<CancellationToken, Task> Work, int MaxRetries, DateTimeOffset? Scheduled)> _normal;
        private readonly Channel<(Func<CancellationToken, Task> Work, int MaxRetries, DateTimeOffset? Scheduled)> _low;
        private readonly ILogger<BackgroundTaskQueue> _logger;

        public BackgroundTaskQueue(ILogger<BackgroundTaskQueue> logger, int capacity = 1000)
        {
            _logger = logger;

            var options = new BoundedChannelOptions(capacity)
            {
                SingleReader = false,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            };

            _high = Channel.CreateBounded<(Func<CancellationToken, Task>, int, DateTimeOffset?)>(options);
            _normal = Channel.CreateBounded<(Func<CancellationToken, Task>, int, DateTimeOffset?)>(options);
            _low = Channel.CreateBounded<(Func<CancellationToken, Task>, int, DateTimeOffset?)>(options);
        }

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem, Core.Interfaces.Background.BackgroundPriority priority = Core.Interfaces.Background.BackgroundPriority.Normal, int maxRetries = 0, DateTimeOffset? scheduled = null)
        {
            if (workItem == null) throw new ArgumentNullException(nameof(workItem));

            var payload = (workItem, maxRetries, scheduled);
            try
            {
                switch (priority)
                {
                    case Core.Interfaces.Background.BackgroundPriority.High:
                        _high.Writer.TryWrite(payload);
                        break;
                    case Core.Interfaces.Background.BackgroundPriority.Low:
                        _low.Writer.TryWrite(payload);
                        break;
                    default:
                        _normal.Writer.TryWrite(payload);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue background work item");
                throw;
            }
        }

        public async Task<(Func<CancellationToken, Task> WorkItem, int MaxRetries, DateTimeOffset? Scheduled)> DequeueAsync(CancellationToken cancellationToken)
        {
            // Prefer high, then normal, then low
            while (!cancellationToken.IsCancellationRequested)
            {
                if (await _high.Reader.WaitToReadAsync(cancellationToken))
                {
                    var item = await _high.Reader.ReadAsync(cancellationToken);
                    return (item.Work, item.MaxRetries, item.Scheduled);
                }

                if (await _normal.Reader.WaitToReadAsync(cancellationToken))
                {
                    var item = await _normal.Reader.ReadAsync(cancellationToken);
                    return (item.Work, item.MaxRetries, item.Scheduled);
                }

                if (await _low.Reader.WaitToReadAsync(cancellationToken))
                {
                    var item = await _low.Reader.ReadAsync(cancellationToken);
                    return (item.Work, item.MaxRetries, item.Scheduled);
                }

                await Task.Delay(50, cancellationToken);
            }

            throw new OperationCanceledException(cancellationToken);
        }
    }
}
