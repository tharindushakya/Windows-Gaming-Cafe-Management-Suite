using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using GamingCafe.Data;
using GamingCafe.Core.Models;

namespace GamingCafe.POS;

public partial class MainWindow : Window
{
    private GamingCafeContext _context = null!;
    private ObservableCollection<Product> _products = new();
    private ObservableCollection<CartItem> _cartItems = new();
    private DispatcherTimer _timer = null!;
    private string _currentCategory = "All";

    public MainWindow()
    {
        InitializeComponent();
        InitializeDatabase();
        InitializeTimer();
        LoadProducts();
        CartListView.ItemsSource = _cartItems;
        UpdateStationInfo();
    }

    private void InitializeDatabase()
    {
        var connectionString = "Server=(localdb)\\mssqllocaldb;Database=GamingCafeDB;Trusted_Connection=true;MultipleActiveResultSets=true";
        var options = new DbContextOptionsBuilder<GamingCafeContext>()
            .UseSqlServer(connectionString)
            .Options;
        
        _context = new GamingCafeContext(options);
        _context.Database.EnsureCreated();
    }

    private void UpdateStationInfo()
    {
        CurrentStationText.Text = "Station: POS Station 1 (POS-001)";
    }

    private void InitializeTimer()
    {
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        CurrentTimeText.Text = DateTime.Now.ToString("HH:mm:ss");
    }

    private async void LoadProducts()
    {
        try
        {
            var products = await _context.Products
                .Where(p => p.IsActive && p.StockQuantity > 0)
                .ToListAsync();

            _products.Clear();
            foreach (var product in products)
            {
                _products.Add(product);
            }

            FilterProducts();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading products: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FilterProducts()
    {
        var filteredProducts = _currentCategory == "All" 
            ? _products 
            : _products.Where(p => p.Category.Equals(_currentCategory, StringComparison.OrdinalIgnoreCase));

        ProductsItemsControl.ItemsSource = filteredProducts;
    }

    private void CategoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string category)
        {
            _currentCategory = category;
            FilterProducts();
        }
    }

    private void ProductCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is Product product)
        {
            AddToCart(product);
        }
    }

    private void AddToCart(Product product)
    {
        var existingItem = _cartItems.FirstOrDefault(i => i.ProductId == product.ProductId);
        
        if (existingItem != null)
        {
            existingItem.Quantity++;
            existingItem.Total = existingItem.Quantity * existingItem.Price;
        }
        else
        {
            _cartItems.Add(new CartItem
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Price = product.Price,
                Quantity = 1,
                Total = product.Price
            });
        }

        UpdateTotals();
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is CartItem item)
        {
            _cartItems.Remove(item);
            UpdateTotals();
        }
    }

    private void UpdateTotals()
    {
        var subtotal = _cartItems.Sum(i => i.Total);
        var tax = subtotal * 0.1m; // Use fixed tax rate for now
        var total = subtotal + tax;

        SubtotalText.Text = subtotal.ToString("C");
        TaxText.Text = tax.ToString("C");
        TotalText.Text = total.ToString("C");
    }

    private async void ProcessPayment_Click(object sender, RoutedEventArgs e)
    {
        if (!_cartItems.Any())
        {
            MessageBox.Show("Cart is empty!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var total = _cartItems.Sum(i => i.Total) * 1.1m; // Include tax
            var paymentDialog = new PaymentDialog(total);
            paymentDialog.Owner = this; // Set owner to prevent new window issue
            
            if (paymentDialog.ShowDialog() == true)
            {
                await ProcessSale(paymentDialog.PaymentMethod);
                _cartItems.Clear();
                UpdateTotals();
                MessageBox.Show("Payment processed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error processing payment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ProcessSale(PaymentMethod paymentMethod)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Create transactions for each item
            foreach (var item in _cartItems)
            {
                var saleTransaction = new Transaction
                {
                    UserId = 1, // Default user for POS sales
                    ProductId = item.ProductId,
                    Description = $"POS Sale - {item.Name}",
                    Amount = item.Total,
                    Type = TransactionType.Product,
                    PaymentMethod = paymentMethod,
                    Status = TransactionStatus.Completed,
                    CreatedAt = DateTime.UtcNow,
                    ProcessedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(saleTransaction);

                // Update inventory
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity -= item.Quantity;
                    
                    var movement = new InventoryMovement
                    {
                        ProductId = item.ProductId,
                        Quantity = -item.Quantity,
                        Type = MovementType.Sale,
                        Reason = "POS Sale",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "POS System"
                    };

                    _context.InventoryMovements.Add(movement);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private void ClearCart_Click(object sender, RoutedEventArgs e)
    {
        _cartItems.Clear();
        UpdateTotals();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Settings window would open here. This includes:\n" +
                       "- Station configuration\n" +
                       "- Tax settings\n" +
                       "- Database connection\n" +
                       "- Security settings", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DailyReport_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Daily Report window would open here. This includes:\n" +
                       "- Sales summary\n" +
                       "- Transaction details\n" +
                       "- Top products\n" +
                       "- Export to CSV", "Daily Report", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void StationControl_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Station Control window would open here. This includes:\n" +
                       "- PC gaming stations management\n" +
                       "- Console stations control\n" +
                       "- Start/end sessions\n" +
                       "- Remote console commands", "Station Control", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer?.Stop();
        _context?.Dispose();
        base.OnClosed(e);
    }
}

public class CartItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal Total { get; set; }
}

public partial class PaymentDialog : Window
{
    public PaymentMethod PaymentMethod { get; private set; }
    private decimal _totalAmount;

    public PaymentDialog(decimal totalAmount)
    {
        _totalAmount = totalAmount;
        InitializeComponent();
        TotalAmountText.Text = totalAmount.ToString("C");
    }

    private void InitializeComponent()
    {
        Title = "Payment";
        Width = 400;
        Height = 300;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Header
        var headerText = new TextBlock
        {
            Text = "Select Payment Method",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 20)
        };
        Grid.SetRow(headerText, 0);
        grid.Children.Add(headerText);

        // Payment methods
        var stackPanel = new StackPanel { Margin = new Thickness(20) };
        
        var totalLabel = new TextBlock { Text = "Total Amount:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) };
        stackPanel.Children.Add(totalLabel);
        
        TotalAmountText = new TextBlock { FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 20) };
        stackPanel.Children.Add(TotalAmountText);

        var cashButton = new Button { Content = "💵 Cash", Margin = new Thickness(0, 5, 0, 0), Padding = new Thickness(10, 5, 10, 5), FontSize = 14 };
        cashButton.Click += (s, e) => { PaymentMethod = PaymentMethod.Cash; DialogResult = true; };
        stackPanel.Children.Add(cashButton);

        var cardButton = new Button { Content = "💳 Credit/Debit Card", Margin = new Thickness(0, 5, 0, 0), Padding = new Thickness(10, 5, 10, 5), FontSize = 14 };
        cardButton.Click += (s, e) => { PaymentMethod = PaymentMethod.CreditCard; DialogResult = true; };
        stackPanel.Children.Add(cardButton);

        Grid.SetRow(stackPanel, 1);
        grid.Children.Add(stackPanel);

        // Cancel button
        var cancelButton = new Button { Content = "Cancel", Margin = new Thickness(20), Padding = new Thickness(10) };
        cancelButton.Click += (s, e) => DialogResult = false;
        Grid.SetRow(cancelButton, 2);
        grid.Children.Add(cancelButton);

        Content = grid;
    }

    private TextBlock TotalAmountText = null!;
}