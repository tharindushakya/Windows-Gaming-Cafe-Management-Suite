using System;
using System.Threading;
using System.Threading.Tasks;

namespace GamingCafe.Core.Interfaces.Background
{
    /// <summary>
    /// Simple background task queue contract for enqueuing work to be processed by a hosted service.
    /// </summary>
    public enum BackgroundPriority
    {
        High = 0,
        Normal = 1,
        Low = 2
    }

    public interface IBackgroundTaskQueue
    {
        /// <summary>
        /// Enqueue a background work item. Optionally set priority, max retries, and scheduled time.
        /// </summary>
    void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem, BackgroundPriority priority = BackgroundPriority.Normal, int maxRetries = 0, DateTimeOffset? scheduled = null);

    /// <summary>
    /// Dequeue the next available background work item according to priority ordering.
    /// </summary>
    Task<(Func<CancellationToken, Task> WorkItem, int MaxRetries, DateTimeOffset? Scheduled)> DequeueAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Current approximate queue lengths by priority. Useful for metrics/health checks.
    /// </summary>
    (int High, int Normal, int Low) GetQueueLengths();

    /// <summary>
    /// Total number of failed background executions since process start.
    /// </summary>
    long GetFailureCount();
    }
}
