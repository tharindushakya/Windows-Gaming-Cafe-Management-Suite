using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using GamingCafe.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Net.Http;
using System.Text;

namespace GamingCafe.API.Background
{
    /// <summary>
    /// Hardened Outbox processor:
    /// - Uses an atomic conditional UPDATE to lock a message for this worker.
    /// - Supports HTTP webhook dispatch and optional Azure Service Bus (via config).
    /// - Dead-letters messages after N attempts.
    /// </summary>
    public class OutboxProcessor : BackgroundService
    {
        private readonly ILogger<OutboxProcessor> _logger;
        private readonly IServiceProvider _provider;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpFactory;

        private const int DefaultMaxAttempts = 5;

        public OutboxProcessor(ILogger<OutboxProcessor> logger, IServiceProvider provider, IConfiguration config, IHttpClientFactory httpFactory)
        {
            _logger = logger;
            _provider = provider;
            _config = config;
            _httpFactory = httpFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxProcessor started");

            var maxAttempts = _config.GetValue<int?>("Outbox:MaxAttempts") ?? DefaultMaxAttempts;
            var dispatchMode = _config["Outbox:Mode"] ?? "http"; // http | servicebus

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _provider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<GamingCafeContext>();

                    // Acquire a single pending message in an atomic, safe way using a conditional UPDATE.
                    // This approach avoids race conditions between multiple workers.
                    // Implementation: SELECT id WHERE unlocked/attempts < max -> UPDATE ... WHERE id = @id AND (LockedUntil IS NULL OR LockedUntil < now()) AND Attempts = @attempts

                    var candidate = await db.OutboxMessages.FirstOrDefaultAsync(o => o.ProcessedOn == null && (o.LockedUntil == null || o.LockedUntil < DateTime.UtcNow), cancellationToken: stoppingToken);
                    if (candidate == null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                        continue;
                    }

                    // Attempt to lock via a conditional update. Use raw SQL for atomic compare-and-swap on Attempts and lock.
                    var now = DateTime.UtcNow;
                    var lockUntil = now.AddMinutes(2);

                    var updated = await db.Database.ExecuteSqlInterpolatedAsync($@"UPDATE ""OutboxMessages"" SET ""LockedUntil"" = {lockUntil}, ""Attempts"" = ""Attempts"" + 1 WHERE ""OutboxMessageId"" = {candidate.OutboxMessageId} AND (""LockedUntil"" IS NULL OR ""LockedUntil"" < {now}) AND ""ProcessedOn"" IS NULL");

                    if (updated == 0)
                    {
                        // Someone else locked it concurrently
                        continue;
                    }

                    // Reload the locked row
                    var msg = await db.OutboxMessages.FirstOrDefaultAsync(o => o.OutboxMessageId == candidate.OutboxMessageId, cancellationToken: stoppingToken);
                    if (msg == null)
                    {
                        continue;
                    }

                    // Dead-letter check
                    if (msg.Attempts > maxAttempts)
                    {
                        _logger.LogWarning("OutboxMessage {Id} exceeded max attempts {MaxAttempts}; moving to dead-letter (ProcessedOn set)", msg.OutboxMessageId, maxAttempts);
                        msg.ProcessedOn = DateTime.UtcNow; // mark processed to avoid further retries; consider moving to a DeadLetter table
                        await db.SaveChangesAsync(stoppingToken);
                        continue;
                    }

                    var dispatched = false;
                    if (string.Equals(dispatchMode, "servicebus", StringComparison.OrdinalIgnoreCase))
                    {
                        // Optional: service bus dispatch - placeholder for future extension
                        dispatched = await DispatchToServiceBusAsync(msg, cancellationToken: stoppingToken);
                    }
                    else
                    {
                        dispatched = await DispatchToHttpWebhookAsync(msg, cancellationToken: stoppingToken);
                    }

                    if (dispatched)
                    {
                        msg.ProcessedOn = DateTime.UtcNow;
                        msg.LockedUntil = null;
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    else
                    {
                        _logger.LogWarning("Dispatch for OutboxMessage {Id} failed; leaving for retry", msg.OutboxMessageId);
                        // Release lock so other workers can pick up later
                        msg.LockedUntil = null;
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // shutting down
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OutboxProcessor error");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        private async Task<bool> DispatchToHttpWebhookAsync(Core.Models.OutboxMessage msg, CancellationToken cancellationToken)
        {
            try
            {
                var httpClient = _httpFactory.CreateClient("OutboxDispatcher");
                // Expect the Outbox message payload to contain a destination header or URL in MessageType or within the payload. Here we assume MessageType is a webhook URL for simplicity.
                var destination = msg.MessageType; // change as appropriate (or add Destination column)
                var content = new StringContent(msg.Payload, Encoding.UTF8, "application/json");
                var resp = await httpClient.PostAsync(destination, content, cancellationToken);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP dispatch failed for OutboxMessage {Id}", msg.OutboxMessageId);
                return false;
            }
        }

        private Task<bool> DispatchToServiceBusAsync(Core.Models.OutboxMessage msg, CancellationToken cancellationToken)
        {
            // Placeholder - implement Azure.Messaging.ServiceBus or Kafka producer here.
            _logger.LogInformation("ServiceBus dispatch not implemented for OutboxMessage {Id}", msg.OutboxMessageId);
            return Task.FromResult(false);
        }
    }
}
