using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models;

public class GameConsole
{
    public int ConsoleId { get; set; }

    [Required]
    [StringLength(50)]
    public string ConsoleName { get; set; } = string.Empty;

    public ConsoleType Type { get; set; } = ConsoleType.PlayStation5;
    
    [StringLength(50)]
    public string Model { get; set; } = string.Empty; // e.g., "PS5 Standard", "Xbox Series X", "Switch OLED"

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

    // Console-specific settings
    public bool IsOnline { get; set; } = true; // For online features
    public bool AllowGameDownloads { get; set; } = true;
    public bool ParentalControlsEnabled { get; set; } = false;
    
    [StringLength(200)]
    public string ControllerSettings { get; set; } = string.Empty; // JSON for controller configs
    
    [StringLength(200)]
    public string DisplaySettings { get; set; } = string.Empty; // JSON for display configs

    public decimal HourlyRate { get; set; } = 5.00m;

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual User? CurrentUser { get; set; }
}

public enum ConsoleType
{
    // PlayStation Family
    PlayStation4 = 10,
    PlayStation5 = 11,
    PlayStation5Pro = 12,
    
    // Xbox Family
    XboxOne = 20,
    XboxOneS = 21,
    XboxOneX = 22,
    XboxSeriesS = 23,
    XboxSeriesX = 24,
    
    // Nintendo Family
    NintendoSwitch = 30,
    NintendoSwitchLite = 31,
    NintendoSwitchOLED = 32,
    
    // PC Gaming
    GamingPC = 40,
    SteamDeck = 41,
    
    // Retro Consoles
    PlayStation3 = 50,
    Xbox360 = 51,
    NintendoWiiU = 52,
    
    // Other
    Custom = 99
}

public enum ConsoleStatus
{
    Offline = 0,
    Online = 1,
    InUse = 2,
    Maintenance = 3,
    Error = 4,
    Updating = 5,
    Downloading = 6,
    Standby = 7
}
