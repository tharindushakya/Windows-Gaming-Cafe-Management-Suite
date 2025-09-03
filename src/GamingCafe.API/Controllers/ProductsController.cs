using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GamingCafe.Data;
using GamingCafe.Core.Models;

namespace GamingCafe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly GamingCafeContext _context;

    public ProductsController(GamingCafeContext context)
    {
        _context = context;
    }

    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
    {
        var products = await _context.Products
            .Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                SKU = p.SKU,
                Category = p.Category,
                Price = p.Price,
                Cost = p.Cost,
                StockQuantity = p.StockQuantity,
                MinStockLevel = p.MinStockLevel,
                IsActive = p.IsActive,
                LoyaltyPointsEarned = p.LoyaltyPointsEarned,
                LoyaltyPointsRequired = p.LoyaltyPointsRequired,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return Ok(products);
    }

    // GET: api/products/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        var productDto = new ProductDto
        {
            ProductId = product.ProductId,
            Name = product.Name,
            Description = product.Description,
            SKU = product.SKU,
            Category = product.Category,
            Price = product.Price,
            Cost = product.Cost,
            StockQuantity = product.StockQuantity,
            MinStockLevel = product.MinStockLevel,
            IsActive = product.IsActive,
            LoyaltyPointsEarned = product.LoyaltyPointsEarned,
            LoyaltyPointsRequired = product.LoyaltyPointsRequired,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };

        return Ok(productDto);
    }

    // GET: api/products/category/{category}
    [HttpGet("category/{category}")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsByCategory(string category)
    {
        var products = await _context.Products
            .Where(p => p.Category.ToLower() == category.ToLower() && p.IsActive)
            .Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                SKU = p.SKU,
                Category = p.Category,
                Price = p.Price,
                Cost = p.Cost,
                StockQuantity = p.StockQuantity,
                MinStockLevel = p.MinStockLevel,
                IsActive = p.IsActive,
                LoyaltyPointsEarned = p.LoyaltyPointsEarned,
                LoyaltyPointsRequired = p.LoyaltyPointsRequired,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return Ok(products);
    }

    // GET: api/products/low-stock
    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetLowStockProducts()
    {
        var products = await _context.Products
            .Where(p => p.StockQuantity <= p.MinStockLevel && p.IsActive)
            .Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                SKU = p.SKU,
                Category = p.Category,
                Price = p.Price,
                Cost = p.Cost,
                StockQuantity = p.StockQuantity,
                MinStockLevel = p.MinStockLevel,
                IsActive = p.IsActive,
                LoyaltyPointsEarned = p.LoyaltyPointsEarned,
                LoyaltyPointsRequired = p.LoyaltyPointsRequired,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return Ok(products);
    }

    // PUT: api/products/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> PutProduct(int id, UpdateProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if SKU is unique (if being changed)
        if (request.SKU != product.SKU && 
            await _context.Products.AnyAsync(p => p.SKU == request.SKU && p.ProductId != id))
        {
            return BadRequest("SKU already exists");
        }

        product.Name = request.Name;
        product.Description = request.Description ?? product.Description;
        product.SKU = request.SKU;
        product.Category = request.Category;
        product.Price = request.Price;
        product.Cost = request.Cost ?? product.Cost;
        product.MinStockLevel = request.MinStockLevel ?? product.MinStockLevel;
        product.IsActive = request.IsActive ?? product.IsActive;
        product.LoyaltyPointsEarned = request.LoyaltyPointsEarned ?? product.LoyaltyPointsEarned;
        product.LoyaltyPointsRequired = request.LoyaltyPointsRequired ?? product.LoyaltyPointsRequired;
        product.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // POST: api/products
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ProductDto>> PostProduct(CreateProductRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if SKU already exists
        if (await _context.Products.AnyAsync(p => p.SKU == request.SKU))
        {
            return BadRequest("SKU already exists");
        }

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description ?? "",
            SKU = request.SKU,
            Category = request.Category,
            Price = request.Price,
            Cost = request.Cost ?? 0,
            StockQuantity = request.StockQuantity ?? 0,
            MinStockLevel = request.MinStockLevel ?? 5,
            IsActive = request.IsActive ?? true,
            LoyaltyPointsEarned = request.LoyaltyPointsEarned ?? 0,
            LoyaltyPointsRequired = request.LoyaltyPointsRequired ?? 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Create initial inventory movement if stock quantity is provided
        if (product.StockQuantity > 0)
        {
            var inventoryMovement = new InventoryMovement
            {
                ProductId = product.ProductId,
                Type = MovementType.StockIn,
                Quantity = product.StockQuantity,
                Notes = "Initial stock",
                CreatedAt = DateTime.UtcNow
            };
            _context.InventoryMovements.Add(inventoryMovement);
            await _context.SaveChangesAsync();
        }

        var productDto = new ProductDto
        {
            ProductId = product.ProductId,
            Name = product.Name,
            Description = product.Description,
            SKU = product.SKU,
            Category = product.Category,
            Price = product.Price,
            Cost = product.Cost,
            StockQuantity = product.StockQuantity,
            MinStockLevel = product.MinStockLevel,
            IsActive = product.IsActive,
            LoyaltyPointsEarned = product.LoyaltyPointsEarned,
            LoyaltyPointsRequired = product.LoyaltyPointsRequired,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };

        return CreatedAtAction("GetProduct", new { id = product.ProductId }, productDto);
    }

    // DELETE: api/products/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        // Soft delete - just deactivate the product
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/products/5/stock/adjust
    [HttpPost("{id}/stock/adjust")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> AdjustStock(int id, StockAdjustmentRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var oldQuantity = product.StockQuantity;
        var newQuantity = oldQuantity + request.QuantityChange;

        if (newQuantity < 0)
        {
            return BadRequest("Insufficient stock");
        }

        product.StockQuantity = newQuantity;
        product.UpdatedAt = DateTime.UtcNow;

        var inventoryMovement = new InventoryMovement
        {
            ProductId = product.ProductId,
            Type = request.QuantityChange > 0 ? MovementType.StockIn : MovementType.StockOut,
            Quantity = Math.Abs(request.QuantityChange),
            Notes = request.Notes ?? "Stock adjustment",
            CreatedAt = DateTime.UtcNow
        };

        _context.InventoryMovements.Add(inventoryMovement);
        await _context.SaveChangesAsync();

        return Ok(new { 
            OldQuantity = oldQuantity, 
            NewQuantity = newQuantity,
            Change = request.QuantityChange
        });
    }

    // GET: api/products/5/inventory-movements
    [HttpGet("{id}/inventory-movements")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<IEnumerable<InventoryMovementDto>>> GetInventoryMovements(int id)
    {
        var movements = await _context.InventoryMovements
            .Where(m => m.ProductId == id)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new InventoryMovementDto
            {
                MovementId = m.MovementId,
                Type = m.Type.ToString(),
                Quantity = m.Quantity,
                Notes = m.Notes,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        return Ok(movements);
    }

    private bool ProductExists(int id)
    {
        return _context.Products.Any(e => e.ProductId == id);
    }
}

public class ProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; }
    public bool IsActive { get; set; }
    public int LoyaltyPointsEarned { get; set; }
    public int LoyaltyPointsRequired { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? Cost { get; set; }
    public int? StockQuantity { get; set; }
    public int? MinStockLevel { get; set; }
    public bool? IsActive { get; set; }
    public int? LoyaltyPointsEarned { get; set; }
    public int? LoyaltyPointsRequired { get; set; }
}

public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? Cost { get; set; }
    public int? MinStockLevel { get; set; }
    public bool? IsActive { get; set; }
    public int? LoyaltyPointsEarned { get; set; }
    public int? LoyaltyPointsRequired { get; set; }
}

public class StockAdjustmentRequest
{
    public int QuantityChange { get; set; }
    public string? Notes { get; set; }
}

public class InventoryMovementDto
{
    public int MovementId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
