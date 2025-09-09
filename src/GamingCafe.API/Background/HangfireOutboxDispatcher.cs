using System;
using System.Threading.Tasks;
using Hangfire;

namespace GamingCafe.API.Background
{
    /// <summary>
    /// Dispatcher that enqueues Hangfire background jobs to process outbox messages.
    /// The actual worker will pick up the OutboxMessage by id and dispatch it.
    /// </summary>
    public class HangfireOutboxDispatcher : IOutboxDispatcher
    {
        public Task DispatchAsync(Guid messageId)
        {
            // Enqueue a fire-and-forget job that calls the static helper to process the outbox message.
            BackgroundJob.Enqueue(() => OutboxWorker.ProcessOutboxMessageAsync(messageId));
            return Task.CompletedTask;
        }
    }
}
