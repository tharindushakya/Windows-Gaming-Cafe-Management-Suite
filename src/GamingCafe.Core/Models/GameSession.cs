using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models;

public class GameSession
{
    public int SessionId { get; set; }

    public int UserId { get; set; }
    public int StationId { get; set; }

    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);

    public decimal HourlyRate { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsPaid { get; set; } = false;
    public SessionStatus Status { get; set; } = SessionStatus.Active;

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual GameStation Station { get; set; } = null!;
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

public enum SessionStatus
{
    Active = 0,
    Paused = 1,
    Completed = 2,
    Cancelled = 3
}
