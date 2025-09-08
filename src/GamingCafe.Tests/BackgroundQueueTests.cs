using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using GamingCafe.API.Background;
using GamingCafe.Core.Interfaces.Background;

namespace GamingCafe.Tests
{
    public class BackgroundQueueTests
    {
        [Fact]
        public async Task Queue_PriorityOrdering_Works()
        {
            var logger = new NullLogger<BackgroundTaskQueue>();
            var queue = new BackgroundTaskQueue(logger, capacity: 10);

            var results = new System.Collections.Concurrent.ConcurrentQueue<string>();

            queue.QueueBackgroundWorkItem(async ct => { results.Enqueue("normal"); await Task.CompletedTask; }, BackgroundPriority.Normal);
            queue.QueueBackgroundWorkItem(async ct => { results.Enqueue("high"); await Task.CompletedTask; }, BackgroundPriority.High);
            queue.QueueBackgroundWorkItem(async ct => { results.Enqueue("low"); await Task.CompletedTask; }, BackgroundPriority.Low);

            // Dequeue three items and ensure high comes first
            var (w1,_,_) = await queue.DequeueAsync(CancellationToken.None);
            await w1(CancellationToken.None);
            var (w2,_,_) = await queue.DequeueAsync(CancellationToken.None);
            await w2(CancellationToken.None);
            var (w3,_,_) = await queue.DequeueAsync(CancellationToken.None);
            await w3(CancellationToken.None);

            // Inspect order
            var arr = results.ToArray();
            Assert.Equal("high", arr[0]);
            Assert.Contains("normal", arr);
            Assert.Contains("low", arr);
        }

        [Fact]
        public async Task HostedService_RetryOnTransient_IncrementsFailureOnNonTransient()
        {
            var logger = new NullLogger<BackgroundTaskQueue>();
            var queue = new BackgroundTaskQueue(logger, capacity: 10);
            var hostedLogger = new NullLogger<QueuedHostedService>();
            var hosted = new QueuedHostedService(queue, hostedLogger);

            var cts = new CancellationTokenSource();

            // Enqueue a work item that throws a transient exception twice then succeeds
            int attempts = 0;
            queue.QueueBackgroundWorkItem(async ct =>
            {
                attempts++;
                if (attempts < 3) throw new TimeoutException("simulated transient");
                await Task.CompletedTask;
            }, BackgroundPriority.Normal, maxRetries: 3);

            // Run hosted service in background
            var runTask = hosted.StartAsync(cts.Token);

            // Allow some time for processing
            await Task.Delay(3000);

            // Stop service
            cts.Cancel();
            await hosted.StopAsync(CancellationToken.None);

            Assert.True(attempts >= 3);

            // Now enqueue a non-transient failure and ensure failure count increments
            var beforeFailures = queue.GetFailureCount();
            queue.QueueBackgroundWorkItem(async ct => { throw new InvalidOperationException("non-transient"); }, BackgroundPriority.Normal, maxRetries: 2);
            cts = new CancellationTokenSource();
            hosted = new QueuedHostedService(queue, hostedLogger);
            await hosted.StartAsync(cts.Token);
            await Task.Delay(2000);
            cts.Cancel();
            await hosted.StopAsync(CancellationToken.None);

            var afterFailures = queue.GetFailureCount();
            Assert.True(afterFailures >= beforeFailures + 1);
        }
    }
}
