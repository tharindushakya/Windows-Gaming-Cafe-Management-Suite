using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using GamingCafe.Core.Models;
using GamingCafe.Core.Interfaces;
using GamingCafe.Data.Repositories;
using System.ComponentModel.DataAnnotations;

namespace GamingCafe.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductsController> _logger;
    private readonly GamingCafe.Core.Interfaces.Services.ICacheService _cacheService;

    public ProductsController(IUnitOfWork unitOfWork, ILogger<ProductsController> logger, GamingCafe.Core.Interfaces.Services.ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Get paginated list of products with filtering and sorting
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductDto>>> GetProducts([FromQuery] GetProductsRequest request)
    {
        try
        {
            // Create cache key that includes search parameters
            var cacheKey = $"products:page_{request.Page}_size_{request.PageSize}_search_{request.Search ?? "none"}_category_{request.Category ?? "none"}_active_{request.IsActive?.ToString() ?? "none"}_minprice_{request.MinPrice?.ToString() ?? "none"}_maxprice_{request.MaxPrice?.ToString() ?? "none"}_instock_{request.InStock?.ToString() ?? "none"}_sortby_{request.SortBy ?? "none"}_desc_{request.SortDescending}";
            var cached = await _cacheService.GetAsync<PagedResponse<ProductDto>>(cacheKey);
            if (cached != null)
            {
                return Ok(cached);
            }

            var products = await _unitOfWork.Repository<Product>().GetAllAsync();
            var filteredProducts = products.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.Search))
            {
                filteredProducts = filteredProducts.Where(p => 
                    p.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(request.Search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(request.Category))
            {
                filteredProducts = filteredProducts.Where(p => 
                    p.Category.Equals(request.Category, StringComparison.OrdinalIgnoreCase));
            }

            if (request.IsActive.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.IsActive == request.IsActive.Value);
            }

            if (request.MinPrice.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.Price >= request.MinPrice.Value);
            }

            if (request.MaxPrice.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.Price <= request.MaxPrice.Value);
            }

            if (request.InStock.HasValue && request.InStock.Value)
            {
                filteredProducts = filteredProducts.Where(p => p.StockQuantity > 0);
            }

            // Apply sorting
            filteredProducts = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending ? 
                    filteredProducts.OrderByDescending(p => p.Name) : 
                    filteredProducts.OrderBy(p => p.Name),
                "price" => request.SortDescending ? 
                    filteredProducts.OrderByDescending(p => p.Price) : 
                    filteredProducts.OrderBy(p => p.Price),
                "stock" => request.SortDescending ? 
                    filteredProducts.OrderByDescending(p => p.StockQuantity) : 
                    filteredProducts.OrderBy(p => p.StockQuantity),
                "category" => request.SortDescending ? 
                    filteredProducts.OrderByDescending(p => p.Category) : 
                    filteredProducts.OrderBy(p => p.Category),
                "createdat" => request.SortDescending ? 
                    filteredProducts.OrderByDescending(p => p.CreatedAt) : 
                    filteredProducts.OrderBy(p => p.CreatedAt),
                _ => filteredProducts.OrderBy(p => p.Name)
            };

            var totalCount = filteredProducts.Count();
            var pagedProducts = filteredProducts
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new ProductDto
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Category = p.Category,
                    StockQuantity = p.StockQuantity,
                    MinStockLevel = p.MinStockLevel,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToList();

            var response = new PagedResponse<ProductDto>
            {
                Data = pagedProducts,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };

            // Cache the response with the specific query parameters for 5 minutes
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products");
            return StatusCode(500, "An error occurred while retrieving products");
        }
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        try
        {
            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id);
            if (product == null)
                return NotFound();

            var productDto = new ProductDto
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Category = product.Category,
                StockQuantity = product.StockQuantity,
                MinStockLevel = product.MinStockLevel,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            // set per-product cache
            var productCacheKey = $"product:{id}";
            await _cacheService.SetAsync(productCacheKey, productDto, TimeSpan.FromMinutes(5));

            return Ok(productDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with ID {ProductId}", id);
            return StatusCode(500, "An error occurred while retrieving the product");
        }
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if product name already exists
            var existingProduct = await _unitOfWork.Repository<Product>()
                .FirstOrDefaultAsync(p => p.Name == request.Name);
            if (existingProduct != null)
                return Conflict("Product with this name already exists");

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description!,
                Price = request.Price,
                Category = request.Category,
                StockQuantity = request.StockQuantity ?? 0,
                MinStockLevel = request.MinStockLevel ?? 0,
                IsActive = request.IsActive ?? true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Product>().AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            var productDto = new ProductDto
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description!,
                Price = product.Price,
                Category = product.Category,
                StockQuantity = product.StockQuantity,
                MinStockLevel = product.MinStockLevel,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            // invalidate product list cache
            await _cacheService.RemoveAsync("products:all");

            _logger.LogInformation("Created new product: {ProductName} with ID {ProductId}", product.Name, product.ProductId);
            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, productDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, "An error occurred while creating the product");
        }
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id);
            if (product == null)
                return NotFound();

            // Check if new name conflicts with existing product (excluding current product)
            if (request.Name != product.Name)
            {
                var existingProduct = await _unitOfWork.Repository<Product>()
                    .FirstOrDefaultAsync(p => p.Name == request.Name && p.ProductId != id);
                if (existingProduct != null)
                    return Conflict("Product with this name already exists");
            }

            // Update product properties
            product.Name = request.Name;
            product.Description = request.Description!;
            product.Price = request.Price;
            product.Category = request.Category;
            product.StockQuantity = request.StockQuantity ?? product.StockQuantity;
            product.MinStockLevel = request.MinStockLevel ?? product.MinStockLevel;
            product.IsActive = request.IsActive ?? product.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Product>().Update(product);
            await _unitOfWork.SaveChangesAsync();

            var productDto = new ProductDto
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description!,
                Price = product.Price,
                Category = product.Category,
                StockQuantity = product.StockQuantity,
                MinStockLevel = product.MinStockLevel,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            // invalidate caches for this product and product list
            await _cacheService.RemoveAsync("products:all");
            await _cacheService.RemoveAsync($"product:{id}");

            _logger.LogInformation("Updated product with ID {ProductId}", id);
            return Ok(productDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with ID {ProductId}", id);
            return StatusCode(500, "An error occurred while updating the product");
        }
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        try
        {
            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id);
            if (product == null)
                return NotFound();

            // Check if product is being used in any transactions
            var allTransactions = await _unitOfWork.Repository<Transaction>().GetAllAsync();
            var transactions = allTransactions.Where(t => t.ProductId == id).ToList();
            
            if (transactions.Any())
            {
                // Soft delete - just mark as inactive
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<Product>().Update(product);
                await _unitOfWork.SaveChangesAsync();
                
                // invalidate caches
                await _cacheService.RemoveAsync("products:all");
                await _cacheService.RemoveAsync($"product:{id}");

                _logger.LogInformation("Soft deleted product with ID {ProductId} (marked as inactive)", id);
                return Ok(new { message = "Product marked as inactive due to existing transaction references" });
            }
            else
            {
                // Hard delete if no transactions reference it
                _unitOfWork.Repository<Product>().Delete(product);
                await _unitOfWork.SaveChangesAsync();
                
                // invalidate caches
                await _cacheService.RemoveAsync("products:all");
                await _cacheService.RemoveAsync($"product:{id}");

                _logger.LogInformation("Hard deleted product with ID {ProductId}", id);
                return NoContent();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID {ProductId}", id);
            return StatusCode(500, "An error occurred while deleting the product");
        }
    }

    /// <summary>
    /// Update product stock quantity
    /// </summary>
    [HttpPatch("{id}/stock")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id);
            if (product == null)
                return NotFound();

            if (request.StockQuantity < 0)
                return BadRequest("Stock quantity cannot be negative");

            var oldStock = product.StockQuantity;
            product.StockQuantity = request.StockQuantity;
            product.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Product>().Update(product);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated stock for product {ProductId} from {OldStock} to {NewStock}", 
                id, oldStock, request.StockQuantity);

            // invalidate product caches
            await _cacheService.RemoveAsync("products:all");
            await _cacheService.RemoveAsync($"product:{id}");

            return Ok(new { 
                ProductId = id, 
                OldStock = oldStock, 
                NewStock = product.StockQuantity,
                IsLowStock = product.StockQuantity <= product.MinStockLevel
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock for product with ID {ProductId}", id);
            return StatusCode(500, "An error occurred while updating product stock");
        }
    }

    /// <summary>
    /// Get low stock products (below minimum stock level)
    /// </summary>
    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<List<ProductDto>>> GetLowStockProducts()
    {
        try
        {
            var allProducts = await _unitOfWork.Repository<Product>().GetAllAsync();
            var products = allProducts.Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel).ToList();

            var lowStockProducts = products.Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Category = p.Category,
                StockQuantity = p.StockQuantity,
                MinStockLevel = p.MinStockLevel,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();

            return Ok(lowStockProducts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving low stock products");
            return StatusCode(500, "An error occurred while retrieving low stock products");
        }
    }

    /// <summary>
    /// Get product categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        try
        {
            var allProducts = await _unitOfWork.Repository<Product>().GetAllAsync();
            var products = allProducts.Where(p => p.IsActive).ToList();
            var categories = products
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product categories");
            return StatusCode(500, "An error occurred while retrieving categories");
        }
    }
}

// DTOs and Request/Response Models
public class ProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class GetProductsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStock { get; set; }
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = false;
}

public class CreateProductRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int? StockQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinStockLevel { get; set; }

    public bool? IsActive { get; set; }
}

public class UpdateProductRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int? StockQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinStockLevel { get; set; }

    public bool? IsActive { get; set; }
}

public class UpdateStockRequest
{
    [Required]
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
}

