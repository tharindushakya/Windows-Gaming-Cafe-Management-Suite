using GamingCafe.Core.Models;
using GamingCafe.Data;
using Microsoft.EntityFrameworkCore;

namespace GamingCafe.Admin.Services;

public interface IPOSService
{
    Task<List<Transaction>> GetTransactionsAsync(DateTime? date = null);
    Task<List<Product>> GetProductsAsync();
    Task<Product> GetProductByIdAsync(int id);
    Task<Product> CreateProductAsync(Product product);
    Task<Product> UpdateProductAsync(Product product);
    Task<bool> DeleteProductAsync(int id);
    Task<POSStats> GetPOSStatsAsync(DateTime? date = null);
    Task<List<Transaction>> ExportTransactionsAsync(DateTime startDate, DateTime endDate);
}

public class POSService : IPOSService
{
    private readonly GamingCafeContext _context;

    public POSService(GamingCafeContext context)
    {
        _context = context;
    }

    public async Task<List<Transaction>> GetTransactionsAsync(DateTime? date = null)
    {
        var query = _context.Transactions
            .Include(t => t.User)
            .AsQueryable();

        if (date.HasValue)
        {
            query = query.Where(t => t.CreatedAt.Date == date.Value.Date);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        return await _context.Products
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product> GetProductByIdAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            throw new ArgumentException($"Product with ID {id} not found");
        
        return product;
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateProductAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<POSStats> GetPOSStatsAsync(DateTime? date = null)
    {
        var targetDate = date ?? DateTime.Today;
        
        var transactions = await _context.Transactions
            .Where(t => t.CreatedAt.Date == targetDate.Date)
            .ToListAsync();

        var totalProducts = await _context.Products.CountAsync();

        return new POSStats
        {
            TodayRevenue = transactions.Sum(t => t.Amount),
            TodayTransactions = transactions.Count,
            AverageTransactionValue = transactions.Any() ? transactions.Average(t => t.Amount) : 0,
            TotalProducts = totalProducts,
            Date = targetDate
        };
    }

    public async Task<List<Transaction>> ExportTransactionsAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Transactions
            .Include(t => t.User)
            .Where(t => t.CreatedAt.Date >= startDate.Date && t.CreatedAt.Date <= endDate.Date)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }
}

public class POSStats
{
    public decimal TodayRevenue { get; set; }
    public int TodayTransactions { get; set; }
    public decimal AverageTransactionValue { get; set; }
    public int TotalProducts { get; set; }
    public DateTime Date { get; set; }
}
