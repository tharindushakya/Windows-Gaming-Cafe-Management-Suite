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
        public Guid OutboxMessageId { get; set; }
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
        public string MessageType { get; set; } = null!;
        public string Payload { get; set; } = null!; // JSON payload
        public DateTime? ProcessedOn { get; set; }
        public DateTime? LockedUntil { get; set; }
        public int Attempts { get; set; }
    }
}
