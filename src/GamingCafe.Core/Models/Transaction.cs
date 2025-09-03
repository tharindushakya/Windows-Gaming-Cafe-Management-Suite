using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models;

public class Transaction
{
    public int TransactionId { get; set; }

    public int UserId { get; set; }
    public int? SessionId { get; set; }
    public int? ProductId { get; set; }

    [Required]
    [StringLength(100)]
    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    [StringLength(100)]
    public string PaymentReference { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual GameSession? Session { get; set; }
    public virtual Product? Product { get; set; }
}

public class WalletTransaction
{
    public int WalletTransactionId { get; set; }

    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public WalletTransactionType Type { get; set; }

    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    public string Reference { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
}

public enum TransactionType
{
    GameTime = 0,
    Product = 1,
    WalletTopup = 2,
    Refund = 3,
    LoyaltyRedemption = 4
}

public enum PaymentMethod
{
    Cash = 0,
    CreditCard = 1,
    DebitCard = 2,
    Wallet = 3,
    LoyaltyPoints = 4,
    BankTransfer = 5
}

public enum TransactionStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Cancelled = 3,
    Refunded = 4
}

public enum WalletTransactionType
{
    Credit = 0,
    Debit = 1,
    Refund = 2
}
