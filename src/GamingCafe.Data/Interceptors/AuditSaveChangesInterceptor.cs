using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using GamingCafe.Core.Models;

namespace GamingCafe.Data.Interceptors;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(Microsoft.EntityFrameworkCore.DbContext? context)
    {
        if (context == null) return;

        var http = _httpContextAccessor?.HttpContext;
        var userId = http?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var requestId = http?.TraceIdentifier;
        var remoteIp = http?.Connection?.RemoteIpAddress?.ToString();
        var userAgent = http?.Request?.Headers["User-Agent"].ToString();
        var correlationId = http?.Request?.Headers["X-Correlation-Id"].ToString();

        var entries = context.ChangeTracker.Entries().Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added || e.State == Microsoft.EntityFrameworkCore.EntityState.Modified || e.State == Microsoft.EntityFrameworkCore.EntityState.Deleted);
        foreach (var entry in entries)
        {
            var now = DateTime.UtcNow;
            // Set timestamps if available
            if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Added)
            {
                if (entry.Metadata.FindProperty("CreatedAt") != null)
                    entry.Property("CreatedAt").CurrentValue = now;
            }
            else
            {
                if (entry.Metadata.FindProperty("UpdatedAt") != null)
                    entry.Property("UpdatedAt").CurrentValue = now;
            }

            try
            {
                // Build a compact change payload
                var payload = new
                {
                    State = entry.State.ToString(),
                    Entity = entry.Entity?.GetType().Name,
                    Key = entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).ToDictionary(p => p.Metadata.Name, p => p.CurrentValue),
                    Changes = entry.Properties.Where(p => p.IsModified).ToDictionary(p => p.Metadata.Name, p => new { Original = p.OriginalValue, Current = p.CurrentValue })
                };

                var json = JsonSerializer.Serialize(payload);
                if (json.Length > 16000) json = json.Substring(0, 16000);

                // Create an AuditLog entry in the same context so it will be saved together
                if (context.Set<AuditLog>() != null)
                {
                    var audit = new AuditLog
                    {
                        Action = entry.State.ToString(),
                        UserId = string.IsNullOrEmpty(userId) ? null : int.Parse(userId),
                        EntityType = entry.Entity?.GetType().Name,
                        EntityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue as int?,
                        Details = json,
                        Timestamp = now,
                        IpAddress = remoteIp,
                        UserAgent = userAgent
                    };

                    // Enrich with tracing identifiers if present
                    if (!string.IsNullOrEmpty(requestId))
                        audit.Details = $"RequestId:{requestId};CorrelationId:{correlationId};" + audit.Details;

                    context.Add(audit);
                }
            }
            catch
            {
                // Non-fatal: do not block saving if auditing fails
            }
        }
    }
}
