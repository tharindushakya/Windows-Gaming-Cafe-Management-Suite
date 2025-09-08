using System;
using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models
{
    public class ScheduledJob
    {
        [Key]
        public Guid JobId { get; set; }

        public string? PayloadType { get; set; }

        public string? PayloadJson { get; set; }

        public DateTimeOffset ScheduledAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public bool Processed { get; set; }
    }
}
