using System.Text.Json;
using System.IO;

namespace GamingCafe.POS;

public class Settings
{
    private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GamingCafePOS", "settings.json");
    
    public string StationName { get; set; } = "POS Station 1";
    public string StationId { get; set; } = "POS-001";
    public decimal TaxRate { get; set; } = 0.10m; // 10%
    public bool IncludeTaxInPrices { get; set; } = false;
    public string CurrencySymbol { get; set; } = "$";
    public bool ShowStockLevels { get; set; } = true;
    public string ConnectionString { get; set; } = "Server=(localdb)\\mssqllocaldb;Database=GamingCafeDB;Trusted_Connection=true;MultipleActiveResultSets=true";
    public string PrinterName { get; set; } = "";
    public bool RequirePasswordForSettings { get; set; } = false;
    public bool RequireManagerApprovalForRefunds { get; set; } = true;
    public bool LogAllTransactions { get; set; } = true;
    public int SessionTimeoutMinutes { get; set; } = 30;
    public bool AutoLogoutOnTimeout { get; set; } = false;
    
    public static Settings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
        
        return new Settings();
    }
    
    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save settings: {ex.Message}");
        }
    }
}
