using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models;

public class InventoryMovement
{
    public int MovementId { get; set; }

    public int ProductId { get; set; }

    [Required]
    public int Quantity { get; set; }

    public MovementType Type { get; set; }

    [Required]
    [StringLength(200)]
    public string Reason { get; set; } = string.Empty;

    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    public int? UserId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    public decimal? UnitCost { get; set; }
    public decimal? TotalCost { get; set; }

    [StringLength(100)]
    public string? Supplier { get; set; }

    [StringLength(100)]
    public string? BatchNumber { get; set; }

    public DateTime? ExpiryDate { get; set; }

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual User? User { get; set; }
}

public enum MovementType
{
    StockIn = 0,
    StockOut = 1,
    Adjustment = 2,
    Sale = 3,
    Return = 4,
    Damage = 5,
    Transfer = 6
}
