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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual User? CurrentUser { get; set; }
    public virtual ICollection<ConsoleSession> Sessions { get; set; } = new List<ConsoleSession>();
    public virtual ICollection<ConsoleRemoteCommand> RemoteCommands { get; set; } = new List<ConsoleRemoteCommand>();
    public virtual ICollection<ConsoleGame> InstalledGames { get; set; } = new List<ConsoleGame>();
}

public class ConsoleSession
{
    public int SessionId { get; set; }

    public int ConsoleId { get; set; }
    public int UserId { get; set; }

    [StringLength(100)]
    public string GameTitle { get; set; } = string.Empty;

    [StringLength(50)]
    public string GameGenre { get; set; } = string.Empty;

    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);

    public decimal HourlyRate { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsPaid { get; set; } = false;

    public SessionStatus Status { get; set; } = SessionStatus.Active;

    // Console-specific session data
    [StringLength(1000)]
    public string SessionData { get; set; } = string.Empty; // JSON for console-specific data
    
    public int PlayersCount { get; set; } = 1; // For local multiplayer tracking

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual GameConsole Console { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

public class ConsoleRemoteCommand
{
    public int CommandId { get; set; }

    public int ConsoleId { get; set; }
    public CommandType Type { get; set; }

    [StringLength(500)]
    public string Command { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Parameters { get; set; } = string.Empty;

    public CommandStatus Status { get; set; } = CommandStatus.Pending;

    [StringLength(1000)]
    public string Response { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExecutedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    [StringLength(500)]
    public string ErrorMessage { get; set; } = string.Empty;

    // Navigation properties
    public virtual GameConsole Console { get; set; } = null!;
}

public class ConsoleGame
{
    public int GameId { get; set; }

    public int ConsoleId { get; set; }

    [Required]
    [StringLength(100)]
    public string GameTitle { get; set; } = string.Empty;

    [StringLength(50)]
    public string Genre { get; set; } = string.Empty;

    [StringLength(20)]
    public string Rating { get; set; } = string.Empty; // ESRB, PEGI, etc.

    public decimal SizeGB { get; set; }
    public DateTime InstallDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastPlayed { get; set; }

    public bool IsInstalled { get; set; } = true;
    public bool IsDownloading { get; set; } = false;
    public decimal DownloadProgress { get; set; } = 100.0m; // Percentage

    [StringLength(200)]
    public string GameImageUrl { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    public string Publisher { get; set; } = string.Empty;

    [StringLength(100)]
    public string Developer { get; set; } = string.Empty;

    public DateTime? ReleaseDate { get; set; }

    // Navigation properties
    public virtual GameConsole Console { get; set; } = null!;
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

public enum CommandType
{
    // Power Management
    PowerOn = 0,
    PowerOff = 1,
    Restart = 2,
    Standby = 3,
    
    // Game Management
    StartGame = 10,
    EndSession = 11,
    PauseGame = 12,
    ResumeGame = 13,
    ScreenCapture = 14,
    
    // System Management
    GetStatus = 20,
    UpdateSystem = 21,
    UpdateGame = 22,
    InstallGame = 23,
    UninstallGame = 24,
    
    // User Management
    LoginUser = 30,
    LogoutUser = 31,
    SwitchUser = 32,
    
    // Settings
    AdjustVolume = 40,
    ChangeDisplaySettings = 41,
    ConfigureNetwork = 42,
    SetParentalControls = 43,
    
    // Remote Control
    NavigateUp = 50,
    NavigateDown = 51,
    NavigateLeft = 52,
    NavigateRight = 53,
    Select = 54,
    Back = 55,
    Home = 56,
    
    // Custom Commands
    Custom = 99
}

public enum CommandStatus
{
    Pending = 0,
    Executing = 1,
    Completed = 2,
    Failed = 3,
    Timeout = 4,
    Cancelled = 5
}
