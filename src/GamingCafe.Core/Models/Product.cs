using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models;

public class Product
{
    public int ProductId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    [StringLength(50)]
    public string SKU { get; set; } = string.Empty;

    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; } = 5;

    [StringLength(200)]
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Loyalty points
    public int LoyaltyPointsEarned { get; set; } = 0;
    public int LoyaltyPointsRequired { get; set; } = 0; // If redeemable with points

    // Navigation properties
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
