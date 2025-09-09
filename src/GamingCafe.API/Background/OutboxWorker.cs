using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GamingCafe.Data;
using System.Text;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;

namespace GamingCafe.API.Background
{
    public static class OutboxWorker
    {
        // This method is executed by Hangfire; keep signature simple.
        public static async Task ProcessOutboxMessageAsync(Guid messageId)
        {
            if (StaticServiceProvider.ServiceProvider == null)
                return;

            using var scope = StaticServiceProvider.ServiceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("OutboxWorker");
            try
            {
                var db = scope.ServiceProvider.GetRequiredService<GamingCafeContext>();
                var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                // OutboxMessage.Id is an int in the model; convert if caller passed a Guid (legacy). Attempt parse.
                if (messageId == Guid.Empty)
                {
                    logger.LogWarning("OutboxWorker called with empty Guid");
                    return;
                }

                // If AggregateId encodes an id, try numeric parse; otherwise, assume caller used Guid to represent int via hash (uncommon).
                // For now, try to find by comparing OccurredOn hash fallback when direct id lookup fails.
                var msg = await db.OutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId.GetHashCode());
                if (msg == null)
                {
                    // Fallback: try to find any message matching AggregateId equals guid string
                    var byAgg = await db.OutboxMessages.FirstOrDefaultAsync(m => m.AggregateId == messageId.ToString());
                    if (byAgg != null)
                        msg = byAgg;
                }
                if (msg == null)
                {
                    logger.LogWarning("Outbox message {Id} not found", messageId);
                    return;
                }

                // Perform HTTP dispatch (same as previous logic)
                var httpClient = httpFactory.CreateClient("OutboxDispatcher");
                var destination = msg.Type;
                var content = new StringContent(msg.Payload ?? string.Empty, Encoding.UTF8, "application/json");
                var resp = await httpClient.PostAsync(destination, content);
                if (resp.IsSuccessStatusCode)
                {
                    msg.Status = GamingCafe.Core.Models.OutboxStatus.Sent;
                    msg.LastAttemptAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
                else
                {
                    msg.Status = GamingCafe.Core.Models.OutboxStatus.Failed;
                    msg.AttemptCount += 1;
                    msg.LastAttemptAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                var logger2 = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("OutboxWorker");
                logger2.LogError(ex, "Error processing outbox message {Id}", messageId);
            }
        }
    }
}
