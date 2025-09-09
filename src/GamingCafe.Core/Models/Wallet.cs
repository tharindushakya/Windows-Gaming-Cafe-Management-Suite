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

    // Optimistic concurrency token to prevent double-spend/data races on Balance updates
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}

public class WalletTransaction
{
    public int WalletTransactionId { get; set; }

    // Legacy property for API compatibility
    public int TransactionId 
    { 
        get => WalletTransactionId; 
        set => WalletTransactionId = value; 
    }

    public int WalletId { get; set; }

    public int UserId { get; set; }

    // Legacy property for API compatibility
    public int RelatedUserId { get; set; }

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

    // Legacy property for API compatibility
    public DateTime CreatedAt 
    { 
        get => TransactionDate; 
        set => TransactionDate = value; 
    }

    public WalletTransactionStatus Status { get; set; } = WalletTransactionStatus.Completed;

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    // Legacy property for API compatibility
    public string? Reference 
    { 
        get => ReferenceNumber; 
        set => ReferenceNumber = value; 
    }

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
    Adjustment = 5,
    Credit = 6,
    Debit = 7
}

public enum WalletTransactionStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Cancelled = 3
}
