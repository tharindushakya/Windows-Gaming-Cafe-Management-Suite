using System;
using System.Windows;
using System.Windows.Controls;

namespace GamingCafe.POS.Windows;

public partial class SettingsPage : UserControl
{
    public event EventHandler? SettingsSaved;
    public event EventHandler? Cancelled;

    private GamingCafe.POS.Settings _settings = null!;

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
        SaveBtn.Click += SaveBtn_Click;
        CancelBtn.Click += (s, e) => Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private void LoadSettings()
    {
        _settings = GamingCafe.POS.Settings.Load();
        StationNameBox.Text = _settings.StationName;
        ConnectionStringBox.Text = _settings.ConnectionString;
        // Select provider in combo (default to Auto when unknown)
        var provider = string.IsNullOrWhiteSpace(_settings.DatabaseProvider) ? "Auto" : _settings.DatabaseProvider;
        foreach (var item in ProviderBox.Items)
        {
            if (item is ComboBoxItem c && string.Equals(c.Content?.ToString(), provider, StringComparison.OrdinalIgnoreCase))
            {
                ProviderBox.SelectedItem = item;
                break;
            }
        }
    }

    private void SaveBtn_Click(object? sender, RoutedEventArgs e)
    {
        _settings.StationName = StationNameBox.Text.Trim();
        _settings.ConnectionString = ConnectionStringBox.Text.Trim();
        if (ProviderBox.SelectedItem is ComboBoxItem sel)
        {
            _settings.DatabaseProvider = sel.Content?.ToString() ?? "Auto";
        }
        _settings.Save();
        SettingsSaved?.Invoke(this, EventArgs.Empty);
    }
}
