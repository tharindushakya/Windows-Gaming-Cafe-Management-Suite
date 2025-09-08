using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models;

public class RefreshToken
{
    [Key]
    public Guid TokenId { get; set; } = Guid.NewGuid();

    public int UserId { get; set; }
    public virtual User? User { get; set; }

    // Store only the hash of the token
    [Required]
    public string TokenHash { get; set; } = string.Empty;

    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    // If this token was rotated, store the new token id
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}
