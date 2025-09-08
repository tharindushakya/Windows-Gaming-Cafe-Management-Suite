using System.ComponentModel.DataAnnotations;
using GamingCafe.Core.Models;

namespace GamingCafe.Core.DTOs;

// Game Station DTOs
public class GameStationDto
{
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string StationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsActive { get; set; }
    public string Processor { get; set; } = string.Empty;
    public string GraphicsCard { get; set; } = string.Empty;
    public string Memory { get; set; } = string.Empty;
    public string Storage { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public UserDto? CurrentUser { get; set; }
    public DateTime? SessionStartTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateStationRequest
{
    [Required]
    [StringLength(50)]
    public string StationName { get; set; } = string.Empty;

    [StringLength(20)]
    public string StationType { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 1000.00)]
    public decimal HourlyRate { get; set; }

    [StringLength(100)]
    public string Processor { get; set; } = string.Empty;

    [StringLength(100)]
    public string GraphicsCard { get; set; } = string.Empty;

    [StringLength(50)]
    public string Memory { get; set; } = string.Empty;

    [StringLength(100)]
    public string Storage { get; set; } = string.Empty;

    [StringLength(15)]
    public string IpAddress { get; set; } = string.Empty;

    [StringLength(17)]
    public string MacAddress { get; set; } = string.Empty;
}

public class UpdateStationRequest
{
    [StringLength(50)]
    public string? StationName { get; set; }

    [StringLength(20)]
    public string? StationType { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0.01, 1000.00)]
    public decimal? HourlyRate { get; set; }

    public bool? IsAvailable { get; set; }
    public bool? IsActive { get; set; }

    [StringLength(100)]
    public string? Processor { get; set; }

    [StringLength(100)]
    public string? GraphicsCard { get; set; }

    [StringLength(50)]
    public string? Memory { get; set; }

    [StringLength(100)]
    public string? Storage { get; set; }

    [StringLength(15)]
    public string? IpAddress { get; set; }

    [StringLength(17)]
    public string? MacAddress { get; set; }
}

// Game Session DTOs
public class GameSessionDto
{
    public int SessionId { get; set; }
    public UserDto User { get; set; } = new();
    public GameStationDto Station { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal TotalCost { get; set; }
    public SessionStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// Product DTOs
public class ProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProductRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 10000.00)]
    public decimal Price { get; set; }

    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int MinStockLevel { get; set; }

    [StringLength(100)]
    public string Barcode { get; set; } = string.Empty;

    [StringLength(200)]
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UpdateProductRequest
{
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0.01, 10000.00)]
    public decimal? Price { get; set; }

    [StringLength(50)]
    public string? Category { get; set; }

    [Range(0, int.MaxValue)]
    public int? StockQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinStockLevel { get; set; }

    [StringLength(100)]
    public string? Barcode { get; set; }

    [StringLength(200)]
    public string? ImageUrl { get; set; }

    public bool? IsActive { get; set; }
}

// Inventory DTOs
public class InventoryMovementDto
{
    public int MovementId { get; set; }
    public ProductDto Product { get; set; } = new();
    public MovementType Type { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public UserDto? User { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateInventoryMovementRequest
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    public MovementType Type { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [StringLength(200)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;

    [StringLength(100)]
    public string ReferenceNumber { get; set; } = string.Empty;
}

// Reservation DTOs
public class ReservationDto
{
    public int ReservationId { get; set; }
    public UserDto User { get; set; } = new();
    public GameStationDto Station { get; set; } = new();
    public DateTime ReservationDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ReservationStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpdateReservationRequest
{
    public DateTime? ReservationDate { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public ReservationStatus? Status { get; set; }
    public string? Notes { get; set; }
}

// Console DTOs
public class GameConsoleDto
{
    public int ConsoleId { get; set; }
    public string ConsoleName { get; set; } = string.Empty;
    public ConsoleType Type { get; set; }
    public string Model { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public ConsoleStatus Status { get; set; }
    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateConsoleRequest
{
    [Required]
    [StringLength(50)]
    public string ConsoleName { get; set; } = string.Empty;

    [Required]
    public ConsoleType Type { get; set; }

    [StringLength(50)]
    public string Model { get; set; } = string.Empty;

    [StringLength(15)]
    public string IpAddress { get; set; } = string.Empty;

    [StringLength(17)]
    public string MacAddress { get; set; } = string.Empty;

    [StringLength(50)]
    public string SerialNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string FirmwareVersion { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 1000.00)]
    public decimal HourlyRate { get; set; }
}

public class UpdateConsoleRequest
{
    [StringLength(50)]
    public string? ConsoleName { get; set; }

    public ConsoleType? Type { get; set; }

    [StringLength(50)]
    public string? Model { get; set; }

    [StringLength(15)]
    public string? IpAddress { get; set; }

    [StringLength(17)]
    public string? MacAddress { get; set; }

    [StringLength(50)]
    public string? SerialNumber { get; set; }

    [StringLength(100)]
    public string? FirmwareVersion { get; set; }

    [Range(0.01, 1000.00)]
    public decimal? HourlyRate { get; set; }

    public ConsoleStatus? Status { get; set; }
    public bool? IsActive { get; set; }
}

// Loyalty DTOs
public class LoyaltyProgramDto
{
    public int ProgramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PointsPerDollar { get; set; }
    public decimal RedemptionValue { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// User DTOs (Admin specific)
public class CreateUserRequest
{
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public decimal InitialWalletBalance { get; set; } = 0;
}

public class UpdateUserRequest
{
    [StringLength(50)]
    public string? Username { get; set; }

    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
}
