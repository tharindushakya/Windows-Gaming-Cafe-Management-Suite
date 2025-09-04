using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models;

public class PS5Console
{
    public int ConsoleId { get; set; }

    [Required]
    [StringLength(50)]
    public string ConsoleName { get; set; } = string.Empty;

    [StringLength(15)]
    public string IpAddress { get; set; } = string.Empty;

    [StringLength(17)]
    public string MacAddress { get; set; } = string.Empty;

    [StringLength(50)]
    public string SerialNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string FirmwareVersion { get; set; } = string.Empty;

    public ConsoleStatus Status { get; set; } = ConsoleStatus.Offline;
    public bool IsAvailable { get; set; } = true;
    public DateTime LastPingAt { get; set; }

    // Current session info
    public int? CurrentUserId { get; set; }
    public DateTime? SessionStartTime { get; set; }

    [StringLength(100)]
    public string CurrentGame { get; set; } = string.Empty;

    // PS5-specific settings
    public bool IsOnline { get; set; } = true;
    public bool AllowGameDownloads { get; set; } = true;
    public bool ParentalControlsEnabled { get; set; } = false;
    
    [StringLength(200)]
    public string ControllerSettings { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string DisplaySettings { get; set; } = string.Empty;

    public decimal HourlyRate { get; set; } = 5.00m;

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual User? CurrentUser { get; set; }
}
