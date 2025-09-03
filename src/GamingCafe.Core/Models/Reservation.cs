using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models;

public class Reservation
{
    public int ReservationId { get; set; }

    public int UserId { get; set; }
    public int StationId { get; set; }

    public DateTime ReservationDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime.Subtract(StartTime);

    public decimal EstimatedCost { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }

    [StringLength(500)]
    public string CancellationReason { get; set; } = string.Empty;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual GameStation Station { get; set; } = null!;
}

public enum ReservationStatus
{
    Pending = 0,
    Confirmed = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    NoShow = 5
}
