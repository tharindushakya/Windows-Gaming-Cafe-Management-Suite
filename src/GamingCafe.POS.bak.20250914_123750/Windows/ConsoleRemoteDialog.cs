using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using GamingCafe.Core.Models;
using GamingCafe.Data;

namespace GamingCafe.POS.Windows;

public partial class ConsoleRemoteDialog : Window
{
    private readonly GameConsole _console;
    private readonly GamingCafeContext _context;
    private readonly ObservableCollection<ConsoleRemoteCommand> _commands = new();
    
    public ConsoleRemoteDialog(GameConsole console, GamingCafeContext context)
    {
        _console = console;
        _context = context;
        InitializeComponent();
        _ = LoadCommands(); // Fire and forget for initialization
    }
    
    private void InitializeComponent()
    {
        Title = $"Remote Control - {_console.ConsoleName}";
        Width = 600;
        Height = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        // Header
        var headerText = new TextBlock
        {
            Text = $"Remote Control - {_console.ConsoleName} ({_console.Model})",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(20, 20, 20, 10)
        };
        Grid.SetRow(headerText, 0);
        grid.Children.Add(headerText);
        
        // Quick Actions
        var actionsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(20, 0, 20, 10)
        };
        
        var powerButton = new Button { Content = "🔌 Power On/Off", Margin = new Thickness(0, 0, 10, 0), Padding = new Thickness(10, 5, 10, 5) };
        powerButton.Click += async (s, e) => await SendCommand(CommandType.PowerOn, "Toggle Power");
        
        var restartButton = new Button { Content = "🔄 Restart", Margin = new Thickness(0, 0, 10, 0), Padding = new Thickness(10, 5, 10, 5) };
        restartButton.Click += async (s, e) => await SendCommand(CommandType.Restart, "Restart");
        
        var gameHomeButton = new Button { Content = "🏠 Home", Margin = new Thickness(0, 0, 10, 0), Padding = new Thickness(10, 5, 10, 5) };
        gameHomeButton.Click += async (s, e) => await SendCommand(CommandType.Home, "Go to Home");
        
        var checkStatusButton = new Button { Content = "📊 Check Status", Padding = new Thickness(10, 5, 10, 5) };
        checkStatusButton.Click += async (s, e) => await SendCommand(CommandType.GetStatus, "Get Status");
        
        actionsPanel.Children.Add(powerButton);
        actionsPanel.Children.Add(restartButton);
        actionsPanel.Children.Add(gameHomeButton);
        actionsPanel.Children.Add(checkStatusButton);
        
        Grid.SetRow(actionsPanel, 1);
        grid.Children.Add(actionsPanel);
        
        // Command History
        var historyLabel = new Label { Content = "Command History:", Margin = new Thickness(20, 10, 20, 0), FontWeight = FontWeights.Bold };
        Grid.SetRow(historyLabel, 2);
        grid.Children.Add(historyLabel);
        
        var commandListView = new ListView
        {
            Margin = new Thickness(20, 0, 20, 10),
            ItemsSource = _commands
        };
        
        var gridView = new GridView();
        gridView.Columns.Add(new GridViewColumn { Header = "Time", DisplayMemberBinding = new System.Windows.Data.Binding("CreatedAt") { StringFormat = "HH:mm:ss" }, Width = 80 });
        gridView.Columns.Add(new GridViewColumn { Header = "Type", DisplayMemberBinding = new System.Windows.Data.Binding("Type"), Width = 80 });
        gridView.Columns.Add(new GridViewColumn { Header = "Command", DisplayMemberBinding = new System.Windows.Data.Binding("Command"), Width = 200 });
        gridView.Columns.Add(new GridViewColumn { Header = "Status", DisplayMemberBinding = new System.Windows.Data.Binding("Status"), Width = 80 });
        gridView.Columns.Add(new GridViewColumn { Header = "Response", DisplayMemberBinding = new System.Windows.Data.Binding("Response"), Width = 120 });
        commandListView.View = gridView;
        
        Grid.SetRow(commandListView, 2);
        grid.Children.Add(commandListView);
        
        // Close Button
        var closeButton = new Button
        {
            Content = "Close",
            Margin = new Thickness(20),
            Padding = new Thickness(20, 8, 20, 8),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        closeButton.Click += (s, e) => Close();
        
        Grid.SetRow(closeButton, 3);
        grid.Children.Add(closeButton);
        
        Content = grid;
    }
    
    private async Task LoadCommands()
    {
        try
        {
            var commands = await _context.ConsoleRemoteCommands
                .Where(c => c.ConsoleId == _console.ConsoleId)
                .OrderByDescending(c => c.CreatedAt)
                .Take(20)
                .ToListAsync();
            
            _commands.Clear();
            foreach (var command in commands)
            {
                _commands.Add(command);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading command history: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async Task SendCommand(CommandType type, string command)
    {
        try
        {
            var remoteCommand = new ConsoleRemoteCommand
            {
                ConsoleId = _console.ConsoleId,
                Type = type,
                Command = command,
                Status = CommandStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = null // System command, no specific user
            };
            
            _context.ConsoleRemoteCommands.Add(remoteCommand);
            await _context.SaveChangesAsync();
            
            // Simulate command execution
            await Task.Delay(1000);
            
            remoteCommand.Status = CommandStatus.Completed;
            remoteCommand.ExecutedAt = DateTime.UtcNow;
            remoteCommand.CompletedAt = DateTime.UtcNow;
            remoteCommand.Response = "Command executed successfully";
            
            await _context.SaveChangesAsync();
            await LoadCommands();
            
            MessageBox.Show($"Command '{command}' sent successfully to {_console.ConsoleName}", 
                          "Command Sent", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error sending command: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
