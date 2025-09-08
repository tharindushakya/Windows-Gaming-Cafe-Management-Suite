using GamingCafe.Core.Interfaces.Background;
using GamingCafe.Data;
using GamingCafe.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace GamingCafe.Data.Repositories
{
    public class ScheduledJobStore : IScheduledJobStore
    {
        private readonly GamingCafeContext _context;

        public ScheduledJobStore(GamingCafeContext context)
        {
            _context = context;
        }

        public async Task SaveScheduledJobAsync(Guid jobId, string payloadType, string payloadJson, DateTimeOffset scheduledAt)
        {
            var job = new ScheduledJob
            {
                JobId = jobId,
                PayloadType = payloadType,
                PayloadJson = payloadJson,
                ScheduledAt = scheduledAt,
                CreatedAt = DateTimeOffset.UtcNow,
                Processed = false
            };

            _context.Add(job);
            await _context.SaveChangesAsync();
        }

        public async Task MarkProcessedAsync(Guid jobId)
        {
            var job = await _context.Set<ScheduledJob>().FindAsync(jobId);
            if (job == null) return;
            job.Processed = true;
            await _context.SaveChangesAsync();
        }
    }
}
