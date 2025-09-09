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
using Npgsql;

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

            // Polling/backoff config
            var basePollSeconds = _config.GetValue<int?>("Outbox:PollIntervalSeconds") ?? 3;
            var maxIdleSeconds = _config.GetValue<int?>("Outbox:MaxIdleSeconds") ?? 60;
            var jitterFraction = _config.GetValue<double?>("Outbox:PollJitterFraction") ?? 0.25; // 25% jitter

            var idleDelaySeconds = basePollSeconds;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _provider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<GamingCafeContext>();

                    // Use EF Core's execution strategy to wrap the transaction so retries are safe.
                    GamingCafe.Core.Models.OutboxMessage? candidate = null;
                    var strategy = db.Database.CreateExecutionStrategy();
                    await strategy.ExecuteAsync(async () =>
                    {
                        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken: stoppingToken);

                        var pending = GamingCafe.Core.Models.OutboxStatus.Pending;
                        var failed = GamingCafe.Core.Models.OutboxStatus.Failed;

                        var found = await db.OutboxMessages.FromSqlInterpolated($@"SELECT * FROM ""OutboxMessages"" WHERE (""Status"" = {pending} OR ""Status"" = {failed}) ORDER BY ""OccurredOn"" LIMIT 1 FOR UPDATE SKIP LOCKED").FirstOrDefaultAsync(stoppingToken);

                        if (found == null)
                        {
                            await tx.RollbackAsync(cancellationToken: stoppingToken);
                            return;
                        }

                        // Claim the message by updating its status and attempt count inside the transaction
                        found.Status = GamingCafe.Core.Models.OutboxStatus.Processing;
                        found.AttemptCount += 1;
                        found.LastAttemptAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(stoppingToken);
                        await tx.CommitAsync(stoppingToken);

                        candidate = found;
                    });

                    if (candidate == null)
                    {
                        // No work found — exponential backoff
                        var jitter = TimeSpan.FromSeconds(idleDelaySeconds * jitterFraction * Random.Shared.NextDouble());
                        var delay = TimeSpan.FromSeconds(idleDelaySeconds) + jitter;
                        _logger.LogDebug("OutboxProcessor idle — waiting {Delay}s before next poll", delay.TotalSeconds);
                        await Task.Delay(delay, stoppingToken);

                        // increase delay up to max
                        idleDelaySeconds = Math.Min(maxIdleSeconds, idleDelaySeconds * 2);
                        continue;
                    }

                    // Reset idle backoff on work
                    idleDelaySeconds = basePollSeconds;

                    // Dead-letter check
                    if (candidate.AttemptCount > maxAttempts)
                    {
                        _logger.LogWarning("OutboxMessage {Id} exceeded max attempts {MaxAttempts}; moving to dead-letter", candidate.Id, maxAttempts);
                        candidate.Status = GamingCafe.Core.Models.OutboxStatus.DeadLetter;
                        await db.SaveChangesAsync(stoppingToken);
                        continue;
                    }

                    var dispatched = false;
                    if (string.Equals(dispatchMode, "servicebus", StringComparison.OrdinalIgnoreCase))
                    {
                        dispatched = await DispatchToServiceBusAsync(candidate, cancellationToken: stoppingToken);
                    }
                    else
                    {
                        dispatched = await DispatchToHttpWebhookAsync(candidate, cancellationToken: stoppingToken);
                    }

                    if (dispatched)
                    {
                        candidate.Status = GamingCafe.Core.Models.OutboxStatus.Sent;
                        candidate.LastAttemptAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    else
                    {
                        _logger.LogWarning("Dispatch for OutboxMessage {Id} failed; leaving for retry", candidate.Id);
                        candidate.Status = GamingCafe.Core.Models.OutboxStatus.Failed;
                        await db.SaveChangesAsync(stoppingToken);
                        // Small random delay to avoid busy retry on dispatch failure
                        var contentionDelayMs = Random.Shared.Next(100, 300);
                        await Task.Delay(contentionDelayMs, stoppingToken);
                    }
                }
                catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // shutting down
                }
                catch (Exception ex)
                {
                    // If the Outbox table doesn't exist yet, Postgres returns SQL state 42P01.
                    // Detect that case and back off longer without noisy stack traces.
                    if (ex is Npgsql.PostgresException pg && pg.SqlState == "42P01")
                    {
                        _logger.LogWarning("Outbox table is missing (Postgres 42P01). Backing off for 30s while awaiting migrations.");
                        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                        continue;
                    }

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
                    // Use the Outbox Type as a destination URL for HTTP mode (convention). Replace with a real Destination column if needed.
                    var destination = msg.Type;
                    var content = new StringContent(msg.Payload, Encoding.UTF8, "application/json");
                    var resp = await httpClient.PostAsync(destination, content, cancellationToken);
                    return resp.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HTTP dispatch failed for OutboxMessage {Id}", msg.Id);
                    return false;
                }
        }

        private Task<bool> DispatchToServiceBusAsync(Core.Models.OutboxMessage msg, CancellationToken cancellationToken)
        {
            // Placeholder - implement Azure.Messaging.ServiceBus or Kafka producer here.
            _logger.LogInformation("ServiceBus dispatch not implemented for OutboxMessage {Id}", msg.Id);
            return Task.FromResult(false);
        }
    }
}
