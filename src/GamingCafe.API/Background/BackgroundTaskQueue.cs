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
        private readonly System.Diagnostics.Metrics.Meter _meter;
        private readonly System.Diagnostics.Metrics.Counter<long> _failureCounter;
        private readonly System.Diagnostics.Metrics.ObservableGauge<int> _highGauge;
        private readonly System.Diagnostics.Metrics.ObservableGauge<int> _normalGauge;
        private readonly System.Diagnostics.Metrics.ObservableGauge<int> _lowGauge;
        private long _failureCount = 0;

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

            _meter = new System.Diagnostics.Metrics.Meter("GamingCafe.BackgroundQueue", "1.0");
            _failureCounter = _meter.CreateCounter<long>("background_tasks.failures", description: "Number of failed background task executions");

            _highGauge = _meter.CreateObservableGauge<int>("background_queue.length.high", () => _high.Reader.Count, description: "Approximate high priority queue length");
            _normalGauge = _meter.CreateObservableGauge<int>("background_queue.length.normal", () => _normal.Reader.Count, description: "Approximate normal priority queue length");
            _lowGauge = _meter.CreateObservableGauge<int>("background_queue.length.low", () => _low.Reader.Count, description: "Approximate low priority queue length");
        }

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem, Core.Interfaces.Background.BackgroundPriority priority = Core.Interfaces.Background.BackgroundPriority.Normal, int maxRetries = 0, DateTimeOffset? scheduled = null)
        {
            if (workItem == null) throw new ArgumentNullException(nameof(workItem));

            var payload = (workItem, maxRetries, scheduled);
            try
            {
                using var activity = GamingCafe.Core.Observability.ActivitySource.StartActivity("BackgroundTaskQueue.Enqueue", System.Diagnostics.ActivityKind.Internal);
                activity?.SetTag("background.priority", priority.ToString());
                activity?.SetTag("background.scheduled", scheduled?.ToString() ?? string.Empty);
                bool written = priority switch
                {
                    Core.Interfaces.Background.BackgroundPriority.High => _high.Writer.TryWrite(payload),
                    Core.Interfaces.Background.BackgroundPriority.Low => _low.Writer.TryWrite(payload),
                    _ => _normal.Writer.TryWrite(payload),
                };

                if (!written)
                {
                    // Since channels are bounded and set to Wait for FullMode, TryWrite may fail in rare races.
                    // Fall back to blocking write to preserve the item but log the contention.
                    _logger.LogWarning("Queue contention detected, performing blocking write for priority {Priority}", priority);
                    switch (priority)
                    {
                        case Core.Interfaces.Background.BackgroundPriority.High:
                            _high.Writer.WriteAsync(payload).AsTask().GetAwaiter().GetResult();
                            break;
                        case Core.Interfaces.Background.BackgroundPriority.Low:
                            _low.Writer.WriteAsync(payload).AsTask().GetAwaiter().GetResult();
                            break;
                        default:
                            _normal.Writer.WriteAsync(payload).AsTask().GetAwaiter().GetResult();
                            break;
                    }
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

        public (int High, int Normal, int Low) GetQueueLengths()
        {
            return (_high.Reader.Count, _normal.Reader.Count, _low.Reader.Count);
        }

        public long GetFailureCount()
        {
            return Interlocked.Read(ref _failureCount);
        }

        internal void IncrementFailure()
        {
            Interlocked.Increment(ref _failureCount);
            _failureCounter.Add(1);
        }
    }
}
