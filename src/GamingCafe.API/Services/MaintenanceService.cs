using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GamingCafe.Data;
using Microsoft.EntityFrameworkCore;

namespace GamingCafe.API.Services
{
    public class MaintenanceService
    {
        private readonly GamingCafeContext _db;
        private readonly ILogger<MaintenanceService> _logger;

        public MaintenanceService(GamingCafeContext db, ILogger<MaintenanceService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<int> CleanupExpiredRefreshTokensAsync()
        {
            var now = DateTime.UtcNow;
            var expired = await _db.RefreshTokens.Where(t => t.ExpiresAt < now && t.RevokedAt == null).ToListAsync();
            foreach (var t in expired)
                t.RevokedAt = now;
            var count = await _db.SaveChangesAsync();
            _logger.LogInformation("Revoked {Count} expired refresh tokens", expired.Count);
            return expired.Count;
        }

        public async Task<int> PurgeOldAuditLogsAsync(int retentionDays)
        {
            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            var old = await _db.AuditLogs.Where(a => a.Timestamp < cutoff).ToListAsync();
            if (old.Count == 0)
                return 0;
            _db.AuditLogs.RemoveRange(old);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Purged {Count} audit logs older than {Days} days", old.Count, retentionDays);
            return old.Count;
        }

        public async Task RecalculateKpiAggregatesAsync()
        {
            // Placeholder: real implementation should compute and persist KPI aggregates.
            _logger.LogInformation("Recalculating KPI aggregates (placeholder)");
            await Task.Delay(10);
        }
    }
}
