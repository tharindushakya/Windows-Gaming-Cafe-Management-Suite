using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models;

public class ConsoleSession
{
    public int SessionId { get; set; }

    public int UserId { get; set; }
    public int ConsoleId { get; set; }
    public int? GameId { get; set; }

    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);

    public decimal HourlyRate { get; set; }
    public decimal TotalCost { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual GameConsole Console { get; set; } = null!;
    public virtual ConsoleGame? Game { get; set; }
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

public class ConsoleRemoteCommand
{
    public int CommandId { get; set; }

    public int ConsoleId { get; set; }
    public int? SessionId { get; set; }

    [Required]
    [StringLength(100)]
    public string Command { get; set; } = string.Empty;

    [StringLength(500)]
    public string Parameters { get; set; } = string.Empty;

    public CommandStatus Status { get; set; } = CommandStatus.Pending;
    public CommandType Type { get; set; }

    [StringLength(1000)]
    public string Response { get; set; } = string.Empty;

    [StringLength(500)]
    public string ErrorMessage { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExecutedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CreatedBy { get; set; }

    // Navigation properties
    public virtual GameConsole Console { get; set; } = null!;
    public virtual ConsoleSession? Session { get; set; }
    public virtual User? Creator { get; set; }
}

public class ConsoleGame
{
    public int GameId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(100)]
    public string Platform { get; set; } = string.Empty;

    [StringLength(50)]
    public string Genre { get; set; } = string.Empty;

    [StringLength(50)]
    public string Rating { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsInstalled { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public decimal? Price { get; set; }
    public int PlayCount { get; set; } = 0;
    public double AverageRating { get; set; } = 0;

    // Additional properties that context expects
    public int? ConsoleId { get; set; }
    public string GameTitle { get; set; } = string.Empty;
    public decimal SizeGB { get; set; }
    public string Publisher { get; set; } = string.Empty;
    public string Developer { get; set; } = string.Empty;
    public DateTime InstallDate { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<ConsoleSession> ConsoleSessions { get; set; } = new List<ConsoleSession>();
}

public enum CommandStatus
{
    Pending = 0,
    Executing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

public enum CommandType
{
    PowerOn = 0,
    PowerOff = 1,
    Restart = 2,
    LaunchGame = 3,
    CloseGame = 4,
    VolumeUp = 5,
    VolumeDown = 6,
    Mute = 7,
    Screenshot = 8,
    RecordVideo = 9,
    Home = 10,
    NavigateSettings = 11,
    GetStatus = 12,
    Custom = 99
}
