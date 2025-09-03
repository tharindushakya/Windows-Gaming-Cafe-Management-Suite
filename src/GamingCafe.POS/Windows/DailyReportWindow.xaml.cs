using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using GamingCafe.Core.Models;
using GamingCafe.Data;

namespace GamingCafe.POS.Windows;

public partial class DailyReportWindow : Window
{
    private readonly GamingCafeContext _context;
    private readonly ObservableCollection<HourlySalesData> _hourlySales = new();
    private readonly ObservableCollection<TopProductData> _topProducts = new();
    private readonly ObservableCollection<TransactionData> _transactions = new();
    
    public DailyReportWindow(GamingCafeContext context)
    {
        InitializeComponent();
        _context = context;
        
        ReportDatePicker.SelectedDate = DateTime.Today;
        HourlySalesListView.ItemsSource = _hourlySales;
        TopProductsListView.ItemsSource = _topProducts;
        TransactionsListView.ItemsSource = _transactions;
        
        _ = LoadReport(); // Fire and forget for initialization
    }
    
    private async void LoadReport_Click(object sender, RoutedEventArgs e)
    {
        await LoadReport();
    }
    
    private async Task LoadReport()
    {
        try
        {
            var selectedDate = ReportDatePicker.SelectedDate ?? DateTime.Today;
            var startDate = selectedDate.Date;
            var endDate = startDate.AddDays(1);
            
            // Load transactions for the selected date
            var transactions = await _context.Transactions
                .Include(t => t.Product)
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt < endDate && t.Status == TransactionStatus.Completed)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            
            // Calculate summary statistics
            var totalSales = transactions.Sum(t => t.Amount);
            var totalTransactions = transactions.Count;
            var averageSale = totalTransactions > 0 ? totalSales / totalTransactions : 0;
            
            TotalSalesText.Text = totalSales.ToString("C");
            TotalTransactionsText.Text = totalTransactions.ToString();
            AverageSaleText.Text = averageSale.ToString("C");
            
            // Top product
            var topProduct = transactions
                .Where(t => t.Product != null)
                .GroupBy(t => t.Product!.Name)
                .OrderByDescending(g => g.Sum(t => t.Amount))
                .FirstOrDefault();
            
            TopProductText.Text = topProduct?.Key ?? "N/A";
            
            // Payment methods breakdown
            var cashTransactions = transactions.Where(t => t.PaymentMethod == PaymentMethod.Cash);
            var cardTransactions = transactions.Where(t => t.PaymentMethod == PaymentMethod.CreditCard || t.PaymentMethod == PaymentMethod.DebitCard);
            
            CashCountText.Text = cashTransactions.Count().ToString();
            CashAmountText.Text = cashTransactions.Sum(t => t.Amount).ToString("C");
            CardCountText.Text = cardTransactions.Count().ToString();
            CardAmountText.Text = cardTransactions.Sum(t => t.Amount).ToString("C");
            
            // Hourly sales breakdown
            _hourlySales.Clear();
            var hourlyData = transactions
                .GroupBy(t => t.CreatedAt.Hour)
                .Select(g => new HourlySalesData
                {
                    Hour = $"{g.Key:00}:00",
                    TransactionCount = g.Count(),
                    TotalSales = g.Sum(t => t.Amount),
                    AverageSale = g.Average(t => t.Amount)
                })
                .OrderBy(h => h.Hour);
            
            foreach (var data in hourlyData)
            {
                _hourlySales.Add(data);
            }
            
            // Top products
            _topProducts.Clear();
            var productData = transactions
                .Where(t => t.Product != null)
                .GroupBy(t => new { t.Product!.Name, t.Product.Category })
                .Select(g => new TopProductData
                {
                    ProductName = g.Key.Name,
                    Category = g.Key.Category,
                    QuantitySold = g.Count(),
                    TotalRevenue = g.Sum(t => t.Amount)
                })
                .OrderByDescending(p => p.TotalRevenue)
                .Take(10);
            
            var rank = 1;
            foreach (var product in productData)
            {
                product.Rank = rank++;
                _topProducts.Add(product);
            }
            
            // All transactions
            _transactions.Clear();
            foreach (var transaction in transactions)
            {
                _transactions.Add(new TransactionData
                {
                    Time = transaction.CreatedAt.ToString("HH:mm"),
                    Description = transaction.Description,
                    Amount = transaction.Amount,
                    PaymentMethod = transaction.PaymentMethod.ToString(),
                    Status = transaction.Status.ToString()
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void ExportReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"DailyReport_{ReportDatePicker.SelectedDate:yyyy-MM-dd}.csv"
            };
            
            if (saveDialog.ShowDialog() == true)
            {
                var csv = new StringBuilder();
                csv.AppendLine("Daily Sales Report");
                csv.AppendLine($"Date: {ReportDatePicker.SelectedDate:yyyy-MM-dd}");
                csv.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                csv.AppendLine();
                
                // Summary
                csv.AppendLine("SUMMARY");
                csv.AppendLine($"Total Sales,{TotalSalesText.Text}");
                csv.AppendLine($"Total Transactions,{TotalTransactionsText.Text}");
                csv.AppendLine($"Average Sale,{AverageSaleText.Text}");
                csv.AppendLine($"Top Product,{TopProductText.Text}");
                csv.AppendLine();
                
                // Payment Methods
                csv.AppendLine("PAYMENT METHODS");
                csv.AppendLine($"Cash Count,{CashCountText.Text}");
                csv.AppendLine($"Cash Amount,{CashAmountText.Text}");
                csv.AppendLine($"Card Count,{CardCountText.Text}");
                csv.AppendLine($"Card Amount,{CardAmountText.Text}");
                csv.AppendLine();
                
                // Hourly Sales
                csv.AppendLine("HOURLY SALES");
                csv.AppendLine("Hour,Transactions,Total Sales,Average Sale");
                foreach (var hour in _hourlySales)
                {
                    csv.AppendLine($"{hour.Hour},{hour.TransactionCount},{hour.TotalSales:C},{hour.AverageSale:C}");
                }
                csv.AppendLine();
                
                // Top Products
                csv.AppendLine("TOP PRODUCTS");
                csv.AppendLine("Rank,Product Name,Category,Quantity Sold,Total Revenue");
                foreach (var product in _topProducts)
                {
                    csv.AppendLine($"{product.Rank},{product.ProductName},{product.Category},{product.QuantitySold},{product.TotalRevenue:C}");
                }
                csv.AppendLine();
                
                // All Transactions
                csv.AppendLine("ALL TRANSACTIONS");
                csv.AppendLine("Time,Description,Amount,Payment Method,Status");
                foreach (var transaction in _transactions)
                {
                    csv.AppendLine($"{transaction.Time},{transaction.Description},{transaction.Amount:C},{transaction.PaymentMethod},{transaction.Status}");
                }
                
                await File.WriteAllTextAsync(saveDialog.FileName, csv.ToString());
                MessageBox.Show("Report exported successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void PrintReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // For now, just show a message. Actual printing would require more complex implementation
                MessageBox.Show("Print functionality would be implemented here with the selected printer.", 
                              "Print Report", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error printing report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

public class HourlySalesData
{
    public string Hour { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AverageSale { get; set; }
}

public class TopProductData
{
    public int Rank { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class TransactionData
{
    public string Time { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
