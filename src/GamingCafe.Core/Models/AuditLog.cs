using GamingCafe.Core.Models;

namespace GamingCafe.Core.Models;

/// <summary>
/// Audit log entity for tracking user actions and system changes
/// </summary>
public class AuditLog
{
    public int AuditLogId { get; set; }
    public string Action { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
