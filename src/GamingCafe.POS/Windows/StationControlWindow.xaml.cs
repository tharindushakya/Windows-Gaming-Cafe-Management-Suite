using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using GamingCafe.Core.Models;
using GamingCafe.Data;

namespace GamingCafe.POS.Windows;

public partial class StationControlWindow : Window
{
    private readonly GamingCafeContext _context;
    private readonly ObservableCollection<GameStation> _gameStations = new();
    private readonly ObservableCollection<GameConsole> _consoleStations = new();
    private readonly DispatcherTimer _refreshTimer;
    
    public StationControlWindow(GamingCafeContext context)
    {
        InitializeComponent();
        _context = context;
        
        GameStationsItemsControl.ItemsSource = _gameStations;
        ConsoleStationsItemsControl.ItemsSource = _consoleStations;
        
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _refreshTimer.Tick += async (s, e) => await RefreshData();
        
        LoadData();
    }
    
    private async void LoadData()
    {
        await RefreshData();
        if (AutoRefreshCheckBox.IsChecked == true)
        {
            _refreshTimer.Start();
        }
    }
    
    private async Task RefreshData()
    {
        try
        {
            StatusText.Text = "Refreshing station data...";
            
            // Load PC Gaming Stations
            var gameStations = await _context.GameStations
                .Include(gs => gs.CurrentUser)
                .Where(gs => gs.IsActive)
                .OrderBy(gs => gs.StationName)
                .ToListAsync();
            
            _gameStations.Clear();
            foreach (var station in gameStations)
            {
                _gameStations.Add(station);
            }
            
            // Load Console Stations
            var consoleStations = await _context.GameConsoles
                .Include(gc => gc.CurrentUser)
                .OrderBy(gc => gc.ConsoleName)
                .ToListAsync();
            
            _consoleStations.Clear();
            foreach (var console in consoleStations)
            {
                _consoleStations.Add(console);
            }
            
            StatusText.Text = $"Ready - {_gameStations.Count} PC stations, {_consoleStations.Count} consoles";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error refreshing data: {ex.Message}";
        }
    }
    
    private async void RefreshStations_Click(object sender, RoutedEventArgs e)
    {
        await RefreshData();
    }
    
    private void AutoRefresh_CheckChanged(object sender, RoutedEventArgs e)
    {
        if (AutoRefreshCheckBox.IsChecked == true)
        {
            _refreshTimer.Start();
        }
        else
        {
            _refreshTimer.Stop();
        }
    }
    
    private async void StartSession_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int stationId)
        {
            try
            {
                var userSelectionDialog = new UserSelectionDialog(_context);
                if (userSelectionDialog.ShowDialog() == true && userSelectionDialog.SelectedUser != null)
                {
                    var station = await _context.GameStations.FindAsync(stationId);
                    if (station != null && station.IsAvailable)
                    {
                        station.CurrentUserId = userSelectionDialog.SelectedUser.UserId;
                        station.SessionStartTime = DateTime.UtcNow;
                        station.IsAvailable = false;
                        
                        // Create a new game session
                        var session = new GameSession
                        {
                            UserId = userSelectionDialog.SelectedUser.UserId,
                            StationId = stationId,
                            StartTime = DateTime.UtcNow,
                            HourlyRate = station.HourlyRate,
                            Status = SessionStatus.Active,
                            CreatedAt = DateTime.UtcNow
                        };
                        
                        _context.GameSessions.Add(session);
                        await _context.SaveChangesAsync();
                        await RefreshData();
                        
                        StatusText.Text = $"Session started for {userSelectionDialog.SelectedUser.Username} on {station.StationName}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting session: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private async void EndSession_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int stationId)
        {
            try
            {
                var station = await _context.GameStations
                    .Include(gs => gs.CurrentUser)
                    .FirstOrDefaultAsync(gs => gs.StationId == stationId);
                
                if (station != null && !station.IsAvailable)
                {
                    var activeSession = await _context.GameSessions
                        .Where(gs => gs.StationId == stationId && gs.Status == SessionStatus.Active)
                        .OrderByDescending(gs => gs.StartTime)
                        .FirstOrDefaultAsync();
                    
                    if (activeSession != null)
                    {
                        var endTime = DateTime.UtcNow;
                        var duration = endTime - activeSession.StartTime;
                        var totalCost = (decimal)duration.TotalHours * activeSession.HourlyRate;
                        
                        activeSession.EndTime = endTime;
                        activeSession.TotalCost = totalCost;
                        activeSession.Status = SessionStatus.Completed;
                        
                        station.CurrentUserId = null;
                        station.SessionStartTime = null;
                        station.IsAvailable = true;
                        
                        await _context.SaveChangesAsync();
                        await RefreshData();
                        
                        MessageBox.Show($"Session ended. Duration: {duration.Hours}h {duration.Minutes}m. Total cost: {totalCost:C}", 
                                      "Session Ended", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        StatusText.Text = $"Session ended on {station.StationName}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error ending session: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private void ConfigureStation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int stationId)
        {
            var station = _gameStations.FirstOrDefault(gs => gs.StationId == stationId);
            if (station != null)
            {
                var configDialog = new StationConfigDialog(station);
                if (configDialog.ShowDialog() == true)
                {
                    // Station configuration would be saved here
                    StatusText.Text = $"Configuration updated for {station.StationName}";
                }
            }
        }
    }
    
    private async void StartConsoleSession_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int consoleId)
        {
            try
            {
                var userSelectionDialog = new UserSelectionDialog(_context);
                if (userSelectionDialog.ShowDialog() == true && userSelectionDialog.SelectedUser != null)
                {
                    var console = await _context.GameConsoles.FindAsync(consoleId);
                    if (console != null && console.IsAvailable)
                    {
                        console.CurrentUserId = userSelectionDialog.SelectedUser.UserId;
                        console.SessionStartTime = DateTime.UtcNow;
                        console.IsAvailable = false;
                        
                        // Create a new console session
                        var session = new ConsoleSession
                        {
                            ConsoleId = consoleId,
                            UserId = userSelectionDialog.SelectedUser.UserId,
                            StartTime = DateTime.UtcNow,
                            HourlyRate = console.HourlyRate,
                            Status = SessionStatus.Active,
                            CreatedAt = DateTime.UtcNow
                        };
                        
                        _context.ConsoleSessions.Add(session);
                        await _context.SaveChangesAsync();
                        await RefreshData();
                        
                        StatusText.Text = $"Console session started for {userSelectionDialog.SelectedUser.Username} on {console.ConsoleName}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting console session: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private async void EndConsoleSession_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int consoleId)
        {
            try
            {
                var console = await _context.GameConsoles
                    .Include(gc => gc.CurrentUser)
                    .FirstOrDefaultAsync(gc => gc.ConsoleId == consoleId);
                
                if (console != null && !console.IsAvailable)
                {
                    var activeSession = await _context.ConsoleSessions
                        .Where(cs => cs.ConsoleId == consoleId && cs.Status == SessionStatus.Active)
                        .OrderByDescending(cs => cs.StartTime)
                        .FirstOrDefaultAsync();
                    
                    if (activeSession != null)
                    {
                        var endTime = DateTime.UtcNow;
                        var duration = endTime - activeSession.StartTime;
                        var totalCost = (decimal)duration.TotalHours * activeSession.HourlyRate;
                        
                        activeSession.EndTime = endTime;
                        activeSession.TotalCost = totalCost;
                        activeSession.Status = SessionStatus.Completed;
                        
                        console.CurrentUserId = null;
                        console.SessionStartTime = null;
                        console.IsAvailable = true;
                        
                        await _context.SaveChangesAsync();
                        await RefreshData();
                        
                        MessageBox.Show($"Console session ended. Duration: {duration.Hours}h {duration.Minutes}m. Total cost: {totalCost:C}", 
                                      "Session Ended", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        StatusText.Text = $"Console session ended on {console.ConsoleName}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error ending console session: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private void RemoteControl_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int consoleId)
        {
            var console = _consoleStations.FirstOrDefault(gc => gc.ConsoleId == consoleId);
            if (console != null)
            {
                var remoteDialog = new ConsoleRemoteDialog(console, _context);
                remoteDialog.ShowDialog();
            }
        }
    }
    
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _refreshTimer?.Stop();
        Close();
    }
    
    protected override void OnClosed(EventArgs e)
    {
        _refreshTimer?.Stop();
        base.OnClosed(e);
    }
}
