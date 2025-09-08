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
        private readonly Channel<Func<CancellationToken, Task>> _queue;
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
            _queue = Channel.CreateBounded<Func<CancellationToken, Task>>(options);
        }

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
        {
            if (workItem == null) throw new ArgumentNullException(nameof(workItem));

            try
            {
                _queue.Writer.TryWrite(workItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue background work item");
                throw;
            }
        }

        public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            var workItem = await _queue.Reader.ReadAsync(cancellationToken);
            return workItem;
        }
    }
}
