using System;

namespace GamingCafe.Core.Models
{
    /// <summary>
    /// Persistent outbox message for reliable cross-process messaging.
    /// The application writes messages here inside the same DB transaction as domain changes.
    /// A background worker reads and dispatches them reliably.
    /// </summary>
    public class OutboxMessage
    {
        public int Id { get; set; }
        public string AggregateId { get; set; } = string.Empty; // e.g., user:123
        public string Type { get; set; } = string.Empty; // semantic type of event
        public string Payload { get; set; } = string.Empty; // JSON payload
        public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
        public int AttemptCount { get; set; } = 0;
        public DateTime? LastAttemptAt { get; set; }
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    }

    public enum OutboxStatus
    {
        Pending = 0,
        Processing = 1,
        Sent = 2,
        Failed = 3,
        DeadLetter = 4
    }
}
