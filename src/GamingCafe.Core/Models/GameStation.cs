using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models;

public class GameStation
{
    public int StationId { get; set; }

    [Required]
    [StringLength(50)]
    public string StationName { get; set; } = string.Empty;

    [StringLength(20)]
    public string StationType { get; set; } = string.Empty; // PC, PS5, Xbox, etc.

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public decimal HourlyRate { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Hardware specifications
    [StringLength(100)]
    public string Processor { get; set; } = string.Empty;
    [StringLength(100)]
    public string GraphicsCard { get; set; } = string.Empty;
    [StringLength(50)]
    public string Memory { get; set; } = string.Empty;
    [StringLength(100)]
    public string Storage { get; set; } = string.Empty;

    // Network information for remote management
    [StringLength(15)]
    public string IpAddress { get; set; } = string.Empty;
    [StringLength(17)]
    public string MacAddress { get; set; } = string.Empty;

    // Current session info
    public int? CurrentUserId { get; set; }
    public DateTime? SessionStartTime { get; set; }

    // Navigation properties
    public virtual User? CurrentUser { get; set; }
    public virtual ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
