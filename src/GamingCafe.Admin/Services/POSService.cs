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
    private readonly IDbContextFactory<GamingCafeContext> _contextFactory;

    public POSService(IDbContextFactory<GamingCafeContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Transaction>> GetTransactionsAsync(DateTime? date = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Transactions
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
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Products
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product> GetProductByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var product = await context.Products.FindAsync(id);
        if (product == null)
            throw new ArgumentException($"Product with ID {id} not found");
        
        return product;
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateProductAsync(Product product)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Products.Update(product);
        await context.SaveChangesAsync();
        return product;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var product = await context.Products.FindAsync(id);
        if (product == null)
            return false;

        context.Products.Remove(product);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<POSStats> GetPOSStatsAsync(DateTime? date = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var targetDate = date ?? DateTime.Today;
        
        var transactions = await context.Transactions
            .Where(t => t.CreatedAt.Date == targetDate.Date)
            .ToListAsync();

        var totalProducts = await context.Products.CountAsync();

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
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Transactions
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
