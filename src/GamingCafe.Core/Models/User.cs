using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models;

public class User
{
    public int UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    public UserRole Role { get; set; } = UserRole.Customer;

    // Security tokens (moved to per-session RefreshToken entity)
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    
    // Email verification
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }
    
    // Two-Factor Authentication
    public bool IsTwoFactorEnabled { get; set; } = false;
    public string? TwoFactorSecretKey { get; set; }
    public string? TwoFactorRecoveryCode { get; set; }

    // Wallet and Loyalty
    public decimal WalletBalance { get; set; } = 0;
    public int LoyaltyPoints { get; set; } = 0;
    public DateTime? MembershipExpiryDate { get; set; }

    // Loyalty Program relationship
    public int? LoyaltyProgramId { get; set; }
    public virtual LoyaltyProgram? LoyaltyProgram { get; set; }

    // Navigation properties
    public virtual ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    // Concurrency token for optimistic concurrency control when updating security-sensitive fields
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Per-device/session refresh tokens
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

public enum UserRole
{
    Customer = 0,
    Staff = 1,
    Manager = 2,
    Admin = 3
}
