using System;
using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models
{
    public class IdempotencyKey
    {
        [Key]
        public string Key { get; set; } = string.Empty;

        public int? UserId { get; set; }

        public string? Endpoint { get; set; }

        public string? RequestHash { get; set; }

        public int? ResponseStatus { get; set; }

        public string? ResponseBody { get; set; }

        public DateTime? ProcessedAt { get; set; }
    }
}
