using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models;

public class LoyaltyProgram
{
    public int ProgramId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    // Legacy property for API compatibility
    public string ProgramName 
    { 
        get => Name; 
        set => Name = value; 
    }

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public int PointsPerDollar { get; set; } = 1;
    public int MinPointsToRedeem { get; set; } = 100;
    public decimal RedemptionValue { get; set; } = 0.01m; // $0.01 per point

    // Additional properties for API compatibility
    public decimal RedemptionRate 
    { 
        get => RedemptionValue; 
        set => RedemptionValue = value; 
    }
    public decimal MinimumSpend { get; set; } = 0;
    public decimal BonusThreshold { get; set; } = 100;
    public decimal BonusMultiplier { get; set; } = 1.5m;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<LoyaltyReward> Rewards { get; set; } = new List<LoyaltyReward>();
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}

public class LoyaltyReward
{
    public int RewardId { get; set; }

    public int ProgramId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public int PointsRequired { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercentage { get; set; }

    [StringLength(200)]
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime? ExpiryDate { get; set; }
    public int? MaxRedemptions { get; set; }
    public int CurrentRedemptions { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual LoyaltyProgram Program { get; set; } = null!;
    public virtual ICollection<LoyaltyRedemption> Redemptions { get; set; } = new List<LoyaltyRedemption>();
}

public class LoyaltyRedemption
{
    public int RedemptionId { get; set; }

    public int UserId { get; set; }
    public int RewardId { get; set; }
    public int PointsUsed { get; set; }

    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual LoyaltyReward Reward { get; set; } = null!;
}
