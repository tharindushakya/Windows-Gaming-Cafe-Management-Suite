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
    private int? _selectedCustomerId = null;
    private Settings _settings = null!;

    // tracked totals (updated by UpdateTotals)
    private decimal _currentSubtotal = 0m;
    private decimal _currentTax = 0m;
    private decimal _currentTotal = 0m;

    public MainWindow()
    {
    InitializeComponent();

    // Load settings early so database / UI honor them
    _settings = Settings.Load();

    // Global UI exception logging
    if (Application.Current != null)
    {
        Application.Current.DispatcherUnhandledException += (s, e) => {
            Logger.Log(e.Exception);
            // allow normal handling (show dialog) after logging
        };
    }

    InitializeDatabase();
    InitializeTimer();
    LoadProducts();
    CartListView.ItemsSource = _cartItems;
    UpdateStationInfo();
    }

    private void ShowSettingsOverlay()
    {
        try
        {
            var page = new GamingCafe.POS.Windows.SettingsPage();
            page.SettingsSaved += (s, e) => {
                // reload settings and update UI
                _settings = Settings.Load();
                UpdateStationInfo();
                UpdateTotals();
                SettingsOverlay.Visibility = Visibility.Collapsed;
                SettingsContentHost.Content = null;
                MessageBox.Show("Settings saved. Some changes may require restart to take full effect.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            };

            page.Cancelled += (s, e) => {
                SettingsOverlay.Visibility = Visibility.Collapsed;
                SettingsContentHost.Content = null;
            };

            SettingsContentHost.Content = page;
            SettingsOverlay.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to open settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InitializeDatabase()
    {
        var connectionString = _settings?.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Server=(localdb)\\mssqllocaldb;Database=GamingCafeDB;Trusted_Connection=true;MultipleActiveResultSets=true";
        }
        var options = new DbContextOptionsBuilder<GamingCafeContext>()
            .UseSqlServer(connectionString)
            .Options;
        
    _context = new GamingCafeContext(options);
    // Intentionally do not call EnsureCreated here. Use EF Core migrations for schema management or a dedicated dev initializer.
    }

    private void UpdateStationInfo()
    {
        if (_settings != null)
        {
            CurrentStationText.Text = $"Station: {_settings.StationName} ({_settings.StationId})";
        }
        else
        {
            CurrentStationText.Text = "Station: POS Station 1 (POS-001)";
        }
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
            var productsQuery = _context.Products.AsQueryable();
            productsQuery = productsQuery.Where(p => p.IsActive);
            if (_settings == null || _settings.ShowStockLevels)
            {
                productsQuery = productsQuery.Where(p => p.StockQuantity > 0);
            }

            var products = await productsQuery.ToListAsync();

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
        _currentSubtotal = _cartItems.Sum(i => i.Total);

        if (_settings != null && _settings.IncludeTaxInPrices)
        {
            // If prices already include tax, extract the tax portion
            _currentTax = _currentSubtotal - (_currentSubtotal / (1 + _settings.TaxRate));
            _currentTotal = _currentSubtotal;
        }
        else
        {
            var taxRate = _settings?.TaxRate ?? 0.10m;
            _currentTax = _currentSubtotal * taxRate;
            _currentTotal = _currentSubtotal + _currentTax;
        }

        // Use configured currency symbol when available
        var currency = _settings?.CurrencySymbol ?? "$";
        SubtotalText.Text = currency + _currentSubtotal.ToString("N2");
        TaxText.Text = currency + _currentTax.ToString("N2");
        TotalText.Text = currency + _currentTotal.ToString("N2");
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
                        CreatedBy = null // System sale, no specific user
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
    // show settings inside the main window as an overlay
    ShowSettingsOverlay();
    }

    private async void SelectCustomerButton_Click(object sender, RoutedEventArgs e)
    {
        // If settings overlay is visible, hide it to ensure dialog ownership behaves correctly
        try
        {
            if (SettingsOverlay.Visibility == Visibility.Visible)
            {
                SettingsOverlay.Visibility = Visibility.Collapsed;
                SettingsContentHost.Content = null;
            }

            var dlg = new SelectCustomerDialog(_context) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _selectedCustomerId = dlg.SelectedCustomerId;
                if (_selectedCustomerId.HasValue)
                {
                    var user = await _context.Users.FindAsync(_selectedCustomerId.Value);
                    var custName = user?.Username ?? "(unknown)";
                    MessageBox.Show($"Selected customer: {custName}", "Customer Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Selected customer id was invalid.", "Customer Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            // Ensure overlay is not left visible
            try { SettingsOverlay.Visibility = Visibility.Collapsed; SettingsContentHost.Content = null; } catch { }
            MessageBox.Show($"Error selecting customer: {ex.Message}\n\nDetails:\n{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
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