using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using GamingCafe.Core.Models;
using GamingCafe.Core.Interfaces;
using GamingCafe.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GamingCafe.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(IUnitOfWork unitOfWork, ILogger<InventoryController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get inventory movements with filtering
    /// </summary>
    [HttpGet("movements")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<PagedResponse<InventoryMovementDto>>> GetInventoryMovements([FromQuery] GetInventoryMovementsRequest request)
    {
        try
        {
            var movements = await _unitOfWork.Repository<InventoryMovement>().GetAllAsync();
            var filteredMovements = movements.AsQueryable();

            // Apply filters
            if (request.ProductId.HasValue)
            {
                filteredMovements = filteredMovements.Where(m => m.ProductId == request.ProductId.Value);
            }

            if (request.MovementType.HasValue)
            {
                filteredMovements = filteredMovements.Where(m => m.Type == request.MovementType.Value);
            }

            if (request.StartDate.HasValue)
            {
                filteredMovements = filteredMovements.Where(m => m.MovementDate >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                filteredMovements = filteredMovements.Where(m => m.MovementDate <= request.EndDate.Value);
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                filteredMovements = filteredMovements.Where(m =>
                    (m.Product != null && !string.IsNullOrEmpty(m.Product.Name) && m.Product.Name.ToLower().Contains(term)) ||
                    (!string.IsNullOrEmpty(m.Reason) && m.Reason.ToLower().Contains(term)));
            }

            // Apply sorting
            filteredMovements = request.SortBy?.ToLower() switch
            {
                "date" => request.SortDescending ? 
                    filteredMovements.OrderByDescending(m => m.MovementDate) : 
                    filteredMovements.OrderBy(m => m.MovementDate),
                "product" => request.SortDescending ? 
                    filteredMovements.OrderByDescending(m => m.Product.Name) : 
                    filteredMovements.OrderBy(m => m.Product.Name),
                "quantity" => request.SortDescending ? 
                    filteredMovements.OrderByDescending(m => m.Quantity) : 
                    filteredMovements.OrderBy(m => m.Quantity),
                "type" => request.SortDescending ? 
                    filteredMovements.OrderByDescending(m => m.Type) : 
                    filteredMovements.OrderBy(m => m.Type),
                _ => filteredMovements.OrderByDescending(m => m.MovementDate)
            };

            // Ensure related entities are included before materializing
            filteredMovements = filteredMovements.Include(m => m.Product).Include(m => m.User);

            var totalCount = filteredMovements.Count();
            var pagedEntities = filteredMovements
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var pagedMovements = pagedEntities.Select(m => new InventoryMovementDto
            {
                MovementId = m.MovementId,
                ProductId = m.ProductId,
                ProductName = m.Product != null && !string.IsNullOrEmpty(m.Product.Name) ? m.Product.Name : "(unknown)",
                Quantity = m.Quantity,
                Type = m.Type.ToString(),
                Reason = m.Reason ?? string.Empty,
                MovementDate = m.MovementDate,
                UserId = m.UserId,
                Username = m.User != null && !string.IsNullOrEmpty(m.User.Username) ? m.User.Username : "System"
            }).ToList();

            var response = new PagedResponse<InventoryMovementDto>
            {
                Data = pagedMovements,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory movements");
            return StatusCode(500, "An error occurred while retrieving inventory movements");
        }
    }

    /// <summary>
    /// Get inventory movement by ID
    /// </summary>
    [HttpGet("movements/{id}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<InventoryMovementDto>> GetInventoryMovement(int id)
    {
        try
        {
            var movement = await _unitOfWork.Repository<InventoryMovement>().GetByIdAsync(id);
            if (movement == null)
                return NotFound();

            var movementDto = new InventoryMovementDto
            {
                MovementId = movement.MovementId,
                ProductId = movement.ProductId,
                ProductName = movement.Product.Name,
                Quantity = movement.Quantity,
                Type = movement.Type.ToString(),
                Reason = movement.Reason,
                MovementDate = movement.MovementDate,
                UserId = movement.UserId,
                Username = movement.User?.Username ?? "System"
            };

            return Ok(movementDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory movement with ID {MovementId}", id);
            return StatusCode(500, "An error occurred while retrieving the inventory movement");
        }
    }

    /// <summary>
    /// Adjust inventory for a product
    /// </summary>
    [HttpPost("adjust")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<InventoryAdjustmentResponseDto>> AdjustInventory([FromBody] AdjustInventoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(request.ProductId);
            if (product == null)
                return NotFound("Product not found");

            // Calculate new stock quantity
            var newQuantity = product.StockQuantity + request.QuantityChange;
            
            if (newQuantity < 0)
                return BadRequest("Adjustment would result in negative stock");

            // Create inventory movement record
            var currentUserId = GetCurrentUserId();
            var movement = new InventoryMovement
            {
                ProductId = request.ProductId,
                Quantity = request.QuantityChange,
                Type = request.QuantityChange > 0 ? MovementType.StockIn : MovementType.StockOut,
                Reason = request.Reason,
                MovementDate = DateTime.UtcNow,
                UserId = currentUserId
            };

            // Update product stock
            var oldQuantity = product.StockQuantity;
            product.StockQuantity = newQuantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Repository<InventoryMovement>().AddAsync(movement);
            _unitOfWork.Repository<Product>().Update(product);
            await _unitOfWork.SaveChangesAsync();

            var response = new InventoryAdjustmentResponseDto
            {
                ProductId = product.ProductId,
                ProductName = product.Name,
                OldQuantity = oldQuantity,
                NewQuantity = newQuantity,
                QuantityChange = request.QuantityChange,
                Reason = request.Reason,
                AdjustedBy = GetCurrentUsername(),
                AdjustedAt = DateTime.UtcNow,
                MovementId = movement.MovementId
            };

            _logger.LogInformation("Inventory adjusted for product {ProductId}: {OldQuantity} -> {NewQuantity} (Change: {Change})", 
                product.ProductId, oldQuantity, newQuantity, request.QuantityChange);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting inventory");
            return StatusCode(500, "An error occurred while adjusting inventory");
        }
    }

    /// <summary>
    /// Bulk inventory adjustment
    /// </summary>
    [HttpPost("bulk-adjust")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<List<InventoryAdjustmentResponseDto>>> BulkAdjustInventory([FromBody] BulkAdjustInventoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var responses = new List<InventoryAdjustmentResponseDto>();
            var movements = new List<InventoryMovement>();
            var productsToUpdate = new List<Product>();

            foreach (var adjustment in request.Adjustments)
            {
                var product = await _unitOfWork.Repository<Product>().GetByIdAsync(adjustment.ProductId);
                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found during bulk adjustment", adjustment.ProductId);
                    continue;
                }

                var newQuantity = product.StockQuantity + adjustment.QuantityChange;
                if (newQuantity < 0)
                {
                    _logger.LogWarning("Adjustment for product {ProductId} would result in negative stock", adjustment.ProductId);
                    continue;
                }

                // Create movement record
                var movement = new InventoryMovement
                {
                    ProductId = adjustment.ProductId,
                    Quantity = adjustment.QuantityChange,
                    Type = adjustment.QuantityChange > 0 ? MovementType.StockIn : MovementType.StockOut,
                    Reason = adjustment.Reason ?? request.DefaultReason ?? "Bulk adjustment",
                    MovementDate = DateTime.UtcNow,
                    UserId = GetCurrentUserId()
                };

                movements.Add(movement);

                // Update product
                var oldQuantity = product.StockQuantity;
                product.StockQuantity = newQuantity;
                product.UpdatedAt = DateTime.UtcNow;
                productsToUpdate.Add(product);

                // Add to response
                responses.Add(new InventoryAdjustmentResponseDto
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    OldQuantity = oldQuantity,
                    NewQuantity = newQuantity,
                    QuantityChange = adjustment.QuantityChange,
                    Reason = movement.Reason,
                    AdjustedBy = GetCurrentUsername(),
                    AdjustedAt = DateTime.UtcNow,
                    MovementId = movement.MovementId
                });
            }

            // Save all changes
            foreach (var movement in movements)
            {
                await _unitOfWork.Repository<InventoryMovement>().AddAsync(movement);
            }

            foreach (var product in productsToUpdate)
            {
                _unitOfWork.Repository<Product>().Update(product);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Bulk inventory adjustment completed for {Count} products", responses.Count);
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk inventory adjustment");
            return StatusCode(500, "An error occurred while performing bulk inventory adjustment");
        }
    }

    /// <summary>
    /// Get low stock products
    /// </summary>
    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<List<InventoryLowStockProductDto>>> GetLowStockProducts()
    {
        try
        {
            var products = await _unitOfWork.Repository<Product>().GetAllAsync();
            var lowStockProducts = products
                .Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel)
                .Select(p => new InventoryLowStockProductDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.Name,
                    Category = p.Category,
                    CurrentStock = p.StockQuantity,
                    MinStockLevel = p.MinStockLevel,
                    UnitPrice = p.Price,
                    Status = p.StockQuantity == 0 ? "Out of Stock" : "Low Stock",
                    DaysOutOfStock = p.StockQuantity == 0 ? CalculateDaysOutOfStock(p.ProductId) : 0
                })
                .OrderBy(p => p.CurrentStock)
                .ToList();

            return Ok(lowStockProducts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving low stock products");
            return StatusCode(500, "An error occurred while retrieving low stock products");
        }
    }

    /// <summary>
    /// Get inventory statistics
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<InventoryStatsDto>> GetInventoryStatistics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.Date.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow.Date.AddDays(1);

            var products = await _unitOfWork.Repository<Product>().GetAllAsync();
            var movements = await _unitOfWork.Repository<InventoryMovement>().GetAllAsync();
            var periodMovements = movements.Where(m => m.MovementDate >= start && m.MovementDate < end);

            var stats = new InventoryStatsDto
            {
                TotalProducts = products.Count(),
                ActiveProducts = products.Count(p => p.IsActive),
                LowStockCount = products.Count(p => p.IsActive && p.StockQuantity <= p.MinStockLevel),
                OutOfStockCount = products.Count(p => p.IsActive && p.StockQuantity == 0),
                TotalInventoryValue = products.Where(p => p.IsActive).Sum(p => p.StockQuantity * p.Price),
                TotalMovements = periodMovements.Count(),
                StockInCount = periodMovements.Count(m => m.Type == MovementType.StockIn),
                StockOutCount = periodMovements.Count(m => m.Type == MovementType.StockOut),
                StartDate = start,
                EndDate = end,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inventory statistics");
            return StatusCode(500, "An error occurred while generating inventory statistics");
        }
    }

    /// <summary>
    /// Get inventory valuation report
    /// </summary>
    [HttpGet("valuation")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<List<ProductValuationDto>>> GetInventoryValuation()
    {
        try
        {
            var products = await _unitOfWork.Repository<Product>().GetAllAsync();
            var valuation = products
                .Where(p => p.IsActive)
                .Select(p => new ProductValuationDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.Name,
                    Category = p.Category,
                    StockQuantity = p.StockQuantity,
                    UnitPrice = p.Price,
                    UnitCost = p.Cost,
                    TotalValue = p.StockQuantity * p.Price,
                    TotalCost = p.StockQuantity * p.Cost,
                    PotentialProfit = (p.StockQuantity * p.Price) - (p.StockQuantity * p.Cost)
                })
                .OrderByDescending(p => p.TotalValue)
                .ToList();

            return Ok(valuation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inventory valuation");
            return StatusCode(500, "An error occurred while generating inventory valuation");
        }
    }

    private int? GetCurrentUserId()
    {
        // TODO: Implement proper user ID extraction from JWT token
        // Return null when running as system/no authenticated user to avoid FK violations
        return null; // Placeholder: no authenticated user in some background or script contexts
    }

    private string GetCurrentUsername()
    {
        // TODO: Implement proper username extraction from JWT token
        return "System"; // Placeholder
    }

    private int CalculateDaysOutOfStock(int productId)
    {
        // TODO: Implement logic to calculate days since product went out of stock
        return 0; // Placeholder
    }
}

// DTOs and Request/Response Models
public class InventoryMovementDto
{
    public int MovementId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime MovementDate { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class InventoryAdjustmentResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int OldQuantity { get; set; }
    public int NewQuantity { get; set; }
    public int QuantityChange { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string AdjustedBy { get; set; } = string.Empty;
    public DateTime AdjustedAt { get; set; }
    public int MovementId { get; set; }
}

public class InventoryLowStockProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinStockLevel { get; set; }
    public decimal UnitPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DaysOutOfStock { get; set; }
}

public class InventoryStatsDto
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public int TotalMovements { get; set; }
    public int StockInCount { get; set; }
    public int StockOutCount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class ProductValuationDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal PotentialProfit { get; set; }
}

public class GetInventoryMovementsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? ProductId { get; set; }
    public MovementType? MovementType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "Date";
    public bool SortDescending { get; set; } = true;
}

public class AdjustInventoryRequest
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(-999999, 999999)]
    public int QuantityChange { get; set; }

    [Required]
    [StringLength(200)]
    public string Reason { get; set; } = string.Empty;
}

public class BulkAdjustInventoryRequest
{
    [Required]
    public List<AdjustInventoryRequest> Adjustments { get; set; } = new();

    [StringLength(200)]
    public string? DefaultReason { get; set; }
}
