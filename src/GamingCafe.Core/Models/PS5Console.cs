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

    public ConsoleStatus Status { get; set; } = ConsoleStatus.Offline;
    public bool IsAvailable { get; set; } = true;
    public DateTime LastPingAt { get; set; }

    // Current session info
    public int? CurrentUserId { get; set; }
    public DateTime? SessionStartTime { get; set; }

    [StringLength(100)]
    public string CurrentGame { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual User? CurrentUser { get; set; }
    public virtual ICollection<PS5Session> Sessions { get; set; } = new List<PS5Session>();
}

public class PS5Session
{
    public int SessionId { get; set; }

    public int ConsoleId { get; set; }
    public int UserId { get; set; }

    [StringLength(100)]
    public string GameTitle { get; set; } = string.Empty;

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
    public virtual PS5Console Console { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

public class PS5RemoteCommand
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

    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    // Navigation properties
    public virtual PS5Console Console { get; set; } = null!;
}

public enum ConsoleStatus
{
    Offline = 0,
    Online = 1,
    InUse = 2,
    Maintenance = 3,
    Error = 4
}

public enum CommandType
{
    PowerOn = 0,
    PowerOff = 1,
    StartGame = 2,
    EndSession = 3,
    GetStatus = 4,
    Screenshot = 5,
    RestartConsole = 6,
    UpdateTime = 7
}

public enum CommandStatus
{
    Pending = 0,
    Executing = 1,
    Completed = 2,
    Failed = 3,
    Timeout = 4
}
