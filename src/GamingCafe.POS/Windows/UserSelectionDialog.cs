using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using GamingCafe.Core.Models;
using GamingCafe.Data;
using System.Linq;

namespace GamingCafe.POS.Windows;

public class UserListItem
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public decimal WalletBalance { get; set; }
    public UserRole Role { get; set; }
}

public partial class UserSelectionDialog : Window
{
    private readonly GamingCafeContext _context;
    private readonly ObservableCollection<UserListItem> _users = new();
    
    public UserListItem? SelectedUser { get; private set; }
    
    public UserSelectionDialog(GamingCafeContext context)
    {
        _context = context;
        InitializeComponent();
        LoadUsers();
    }
    
    private void InitializeComponent()
    {
        Title = "Select User";
        Width = 400;
        Height = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        // Header
        var headerText = new TextBlock
        {
            Text = "Select a user to start the session:",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(20, 20, 20, 10)
        };
        Grid.SetRow(headerText, 0);
        grid.Children.Add(headerText);
        
        // User list
        var userListView = new ListView
        {
            Margin = new Thickness(20, 10, 20, 10),
            ItemsSource = _users
        };
        
        userListView.SelectionChanged += UserListView_SelectionChanged;
        
    var gridView = new GridView();
    gridView.Columns.Add(new GridViewColumn { Header = "Username", DisplayMemberBinding = new System.Windows.Data.Binding("Username"), Width = 120 });
    gridView.Columns.Add(new GridViewColumn { Header = "Name", DisplayMemberBinding = new System.Windows.Data.Binding("FirstName"), Width = 100 });
    gridView.Columns.Add(new GridViewColumn { Header = "Wallet", DisplayMemberBinding = new System.Windows.Data.Binding("WalletBalance") { StringFormat = "C" }, Width = 80 });
        userListView.View = gridView;
        
        Grid.SetRow(userListView, 1);
        grid.Children.Add(userListView);
        
        // Buttons
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(20)
        };
        
        var selectButton = new Button
        {
            Content = "Select",
            Margin = new Thickness(0, 0, 10, 0),
            Padding = new Thickness(20, 8, 20, 8),
            IsEnabled = false
        };
        selectButton.Click += SelectButton_Click;
        
        var cancelButton = new Button
        {
            Content = "Cancel",
            Padding = new Thickness(20, 8, 20, 8)
        };
        cancelButton.Click += (s, e) => DialogResult = false;
        
        buttonPanel.Children.Add(selectButton);
        buttonPanel.Children.Add(cancelButton);
        
        Grid.SetRow(buttonPanel, 2);
        grid.Children.Add(buttonPanel);
        
        Content = grid;
        
        // Store reference to select button for enabling/disabling
        Tag = selectButton;
    }
    
    private async void LoadUsers()
    {
        try
        {
            var users = await _context.Users
                    .Where(u => u.IsActive && u.Role != UserRole.Admin)
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                var userIds = users.Select(u => u.UserId).ToList();
                var wallets = await _context.Wallets.Where(w => userIds.Contains(w.UserId)).ToListAsync();
                var walletMap = wallets.ToDictionary(w => w.UserId, w => w.Balance);

                foreach (var user in users)
                {
                    _users.Add(new UserListItem
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        FirstName = user.FirstName,
                        WalletBalance = walletMap.TryGetValue(user.UserId, out var b) ? b : 0m,
                        Role = user.Role
                    });
                }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading users: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void UserListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && Tag is Button selectButton)
        {
            selectButton.IsEnabled = listView.SelectedItem != null;
            SelectedUser = listView.SelectedItem as UserListItem;
        }
    }
    
    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
