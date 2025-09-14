using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using GamingCafe.Data;

namespace GamingCafe.POS.Windows
{
    public partial class SettingsPage : UserControl
    {
        private readonly Settings _settings;

        public event EventHandler? SettingsSaved;
        public event EventHandler? Cancelled;

        public SettingsPage()
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
                UserIdColumnTextBox.Text = _settings.UserIdColumnName;
                UsernameColumnTextBox.Text = _settings.UsernameColumnName;
                EmailColumnTextBox.Text = _settings.EmailColumnName;
                UserPageSizeTextBox.Text = _settings.UserQueryPageSize.ToString();
                EnableUserPagingCheckBox.IsChecked = _settings.EnableUserPaging;
            RequirePasswordCheckBox.IsChecked = _settings.RequirePasswordForSettings;
            RequireManagerApprovalCheckBox.IsChecked = _settings.RequireManagerApprovalForRefunds;
            LogAllTransactionsCheckBox.IsChecked = _settings.LogAllTransactions;
            SessionTimeoutTextBox.Text = _settings.SessionTimeoutMinutes.ToString();
            AutoLogoutCheckBox.IsChecked = _settings.AutoLogoutOnTimeout;

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
                    _settings.UserIdColumnName = UserIdColumnTextBox.Text?.Trim() ?? string.Empty;
                    _settings.UsernameColumnName = UsernameColumnTextBox.Text?.Trim() ?? string.Empty;
                    _settings.EmailColumnName = EmailColumnTextBox.Text?.Trim() ?? string.Empty;
                    if (int.TryParse(UserPageSizeTextBox.Text, out var pageSize)) _settings.UserQueryPageSize = Math.Max(50, pageSize);
                    _settings.EnableUserPaging = EnableUserPagingCheckBox.IsChecked ?? true;
                _settings.RequirePasswordForSettings = RequirePasswordCheckBox.IsChecked ?? false;
                _settings.RequireManagerApprovalForRefunds = RequireManagerApprovalCheckBox.IsChecked ?? false;
                _settings.LogAllTransactions = LogAllTransactionsCheckBox.IsChecked ?? false;

                if (int.TryParse(SessionTimeoutTextBox.Text, out var timeout))
                    _settings.SessionTimeoutMinutes = timeout;

                _settings.AutoLogoutOnTimeout = AutoLogoutCheckBox.IsChecked ?? false;
                _settings.PrinterName = PrinterComboBox.SelectedIndex == 0 ? "Default" : "";

                _settings.Save();

                SettingsSaved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }
}
