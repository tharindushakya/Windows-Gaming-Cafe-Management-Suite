using System;
using System.Threading.Tasks;

namespace GamingCafe.Core.Interfaces.Background
{
    public interface IScheduledJobStore
    {
        Task SaveScheduledJobAsync(Guid jobId, string payloadType, string payloadJson, DateTimeOffset scheduledAt);
        Task MarkProcessedAsync(Guid jobId);
    }
}
