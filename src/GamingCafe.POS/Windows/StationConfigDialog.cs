using System.Windows;
using System.Windows.Controls;
using GamingCafe.Core.Models;

namespace GamingCafe.POS.Windows;

public partial class StationConfigDialog : Window
{
    private readonly GameStation _station;
    
    public StationConfigDialog(GameStation station)
    {
        _station = station;
        InitializeComponent();
        LoadStationData();
    }
    
    private void InitializeComponent()
    {
        Title = $"Configure {_station.StationName}";
        Width = 500;
        Height = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        // Header
        var headerText = new TextBlock
        {
            Text = $"Station Configuration - {_station.StationName}",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(20, 20, 20, 10)
        };
        Grid.SetRow(headerText, 0);
        grid.Children.Add(headerText);
        
        // Configuration form
        var formPanel = new StackPanel { Margin = new Thickness(20) };
        
        // Station Name
        formPanel.Children.Add(new Label { Content = "Station Name:" });
        var nameTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(5) };
        formPanel.Children.Add(nameTextBox);
        
        // Hourly Rate
        formPanel.Children.Add(new Label { Content = "Hourly Rate:" });
        var rateTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(5) };
        formPanel.Children.Add(rateTextBox);
        
        // Description
        formPanel.Children.Add(new Label { Content = "Description:" });
        var descriptionTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(5), Height = 60, TextWrapping = TextWrapping.Wrap };
        formPanel.Children.Add(descriptionTextBox);
        
        // IP Address
        formPanel.Children.Add(new Label { Content = "IP Address:" });
        var ipTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(5) };
        formPanel.Children.Add(ipTextBox);
        
        // Is Active
        var activeCheckBox = new CheckBox { Content = "Station is active", Margin = new Thickness(0, 10, 0, 0) };
        formPanel.Children.Add(activeCheckBox);
        
        Grid.SetRow(formPanel, 1);
        grid.Children.Add(formPanel);
        
        // Buttons
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(20)
        };
        
        var saveButton = new Button
        {
            Content = "Save",
            Margin = new Thickness(0, 0, 10, 0),
            Padding = new Thickness(20, 8, 20, 8)
        };
        saveButton.Click += (s, e) =>
        {
            // Save configuration logic would go here
            DialogResult = true;
        };
        
        var cancelButton = new Button
        {
            Content = "Cancel",
            Padding = new Thickness(20, 8, 20, 8)
        };
        cancelButton.Click += (s, e) => DialogResult = false;
        
        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(cancelButton);
        
        Grid.SetRow(buttonPanel, 2);
        grid.Children.Add(buttonPanel);
        
        Content = grid;
        
        // Store references for data binding
        Tag = new { nameTextBox, rateTextBox, descriptionTextBox, ipTextBox, activeCheckBox };
    }
    
    private void LoadStationData()
    {
        if (Tag is not null)
        {
            dynamic controls = Tag;
            controls.nameTextBox.Text = _station.StationName;
            controls.rateTextBox.Text = _station.HourlyRate.ToString("F2");
            controls.descriptionTextBox.Text = _station.Description;
            controls.ipTextBox.Text = _station.IpAddress;
            controls.activeCheckBox.IsChecked = _station.IsActive;
        }
    }
}
