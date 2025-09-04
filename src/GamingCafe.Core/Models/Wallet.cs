using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models;

public class Wallet
{
    public int WalletId { get; set; }

    public int UserId { get; set; }

    [Required]
    public decimal Balance { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    [StringLength(50)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}

public class WalletTransaction
{
    public int WalletTransactionId { get; set; }

    public int WalletId { get; set; }

    public int UserId { get; set; }

    public WalletTransactionType Type { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }

    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    [StringLength(100)]
    public string ProcessedBy { get; set; } = string.Empty;

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    public WalletTransactionStatus Status { get; set; } = WalletTransactionStatus.Completed;

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    public int? RelatedTransactionId { get; set; }

    // Navigation properties
    public virtual Wallet Wallet { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual WalletTransaction? RelatedTransaction { get; set; }
}

public enum WalletTransactionType
{
    Deposit = 0,
    Withdrawal = 1,
    Transfer = 2,
    Purchase = 3,
    Refund = 4,
    Adjustment = 5
}

public enum WalletTransactionStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Cancelled = 3
}
