using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using GamingCafe.Data;

namespace GamingCafe.POS.Windows;

public partial class SettingsWindow : Window
{
    private readonly Settings _settings;
    
    public SettingsWindow()
    {
        InitializeComponent();
        _settings = Settings.Load();
        LoadSettings();
    }
    
    private void LoadSettings()
    {
        StationNameTextBox.Text = _settings.StationName;
        StationIdTextBox.Text = _settings.StationId;
        TaxRateTextBox.Text = (_settings.TaxRate * 100).ToString("F2");
        IncludeTaxCheckBox.IsChecked = _settings.IncludeTaxInPrices;
        CurrencySymbolTextBox.Text = _settings.CurrencySymbol;
        ShowStockLevelsCheckBox.IsChecked = _settings.ShowStockLevels;
        ConnectionStringTextBox.Text = _settings.ConnectionString;
        RequirePasswordCheckBox.IsChecked = _settings.RequirePasswordForSettings;
        RequireManagerApprovalCheckBox.IsChecked = _settings.RequireManagerApprovalForRefunds;
        LogAllTransactionsCheckBox.IsChecked = _settings.LogAllTransactions;
        SessionTimeoutTextBox.Text = _settings.SessionTimeoutMinutes.ToString();
        AutoLogoutCheckBox.IsChecked = _settings.AutoLogoutOnTimeout;
        
        // Set printer selection
        PrinterComboBox.SelectedIndex = string.IsNullOrEmpty(_settings.PrinterName) ? 1 : 0;
    }
    
    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var connectionString = ConnectionStringTextBox.Text;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                ConnectionStatusText.Text = "Please enter a connection string";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }
            
            var options = new DbContextOptionsBuilder<GamingCafeContext>()
                .UseSqlServer(connectionString)
                .Options;
                
            using var context = new GamingCafeContext(options);
            await context.Database.CanConnectAsync();
            
            ConnectionStatusText.Text = "✅ Connection successful!";
            ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Green;
        }
        catch (Exception ex)
        {
            ConnectionStatusText.Text = $"❌ Connection failed: {ex.Message}";
            ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Red;
        }
    }
    
    private void ResetConnection_Click(object sender, RoutedEventArgs e)
    {
        ConnectionStringTextBox.Text = "Server=(localdb)\\mssqllocaldb;Database=GamingCafeDB;Trusted_Connection=true;MultipleActiveResultSets=true";
        ConnectionStatusText.Text = "";
    }
    
    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings.StationName = StationNameTextBox.Text;
            _settings.StationId = StationIdTextBox.Text;
            
            if (decimal.TryParse(TaxRateTextBox.Text, out var taxRate))
                _settings.TaxRate = taxRate / 100;
                
            _settings.IncludeTaxInPrices = IncludeTaxCheckBox.IsChecked ?? false;
            _settings.CurrencySymbol = CurrencySymbolTextBox.Text;
            _settings.ShowStockLevels = ShowStockLevelsCheckBox.IsChecked ?? false;
            _settings.ConnectionString = ConnectionStringTextBox.Text;
            _settings.RequirePasswordForSettings = RequirePasswordCheckBox.IsChecked ?? false;
            _settings.RequireManagerApprovalForRefunds = RequireManagerApprovalCheckBox.IsChecked ?? false;
            _settings.LogAllTransactions = LogAllTransactionsCheckBox.IsChecked ?? false;
            
            if (int.TryParse(SessionTimeoutTextBox.Text, out var timeout))
                _settings.SessionTimeoutMinutes = timeout;
                
            _settings.AutoLogoutOnTimeout = AutoLogoutCheckBox.IsChecked ?? false;
            _settings.PrinterName = PrinterComboBox.SelectedIndex == 0 ? "Default" : "";
            
            _settings.Save();
            
            MessageBox.Show("Settings saved successfully! Please restart the application for some changes to take effect.", 
                          "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
