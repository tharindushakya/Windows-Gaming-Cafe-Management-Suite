using System;
using System.Threading;
using System.Threading.Tasks;

namespace GamingCafe.Core.Interfaces.Background
{
    /// <summary>
    /// Simple background task queue contract for enqueuing work to be processed by a hosted service.
    /// </summary>
    public interface IBackgroundTaskQueue
    {
        /// <summary>
        /// Enqueue a background work item. The work item receives a CancellationToken and returns a Task.
        /// </summary>
        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);

        /// <summary>
        /// Dequeue a background work item. This method blocks until an item is available or token is cancelled.
        /// </summary>
        Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
    }
}
