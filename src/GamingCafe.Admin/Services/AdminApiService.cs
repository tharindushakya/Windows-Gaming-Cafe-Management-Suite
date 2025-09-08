using GamingCafe.Core.DTOs;
using GamingCafe.Core.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GamingCafe.Admin.Services;

public class AdminApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly AdminAuthService _authService;
    private string? _baseUrl;

    public AdminApiService(HttpClient httpClient, IConfiguration configuration, AdminAuthService authService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _authService = authService;
        _baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001";
    }

    private async Task<bool> EnsureAuthenticatedAsync()
    {
        var token = await _authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }
        
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return true;
    }

    private async Task<T?> GetAsync<T>(string endpoint)
    {
        if (!await EnsureAuthenticatedAsync())
            return default;

        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/{endpoint}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }
        catch (Exception ex)
        {
            // Log exception
            Console.WriteLine($"Error in GetAsync: {ex.Message}");
        }
        
        return default;
    }

    private async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        if (!await EnsureAuthenticatedAsync())
            return default;

        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/{endpoint}", content);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in PostAsync: {ex.Message}");
        }
        
        return default;
    }

    private async Task<bool> PutAsync(string endpoint, object data)
    {
        if (!await EnsureAuthenticatedAsync())
            return false;

        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"{_baseUrl}/api/{endpoint}", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in PutAsync: {ex.Message}");
        }
        
        return false;
    }

    private async Task<bool> DeleteAsync(string endpoint)
    {
        if (!await EnsureAuthenticatedAsync())
            return false;

        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/{endpoint}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DeleteAsync: {ex.Message}");
        }
        
        return false;
    }

    // Users
    public async Task<List<UserDto>?> GetUsersAsync() => await GetAsync<List<UserDto>>("users");
    public async Task<UserDto?> GetUserAsync(int id) => await GetAsync<UserDto>($"users/{id}");
    public async Task<UserDto?> CreateUserAsync(CreateUserRequest request) => await PostAsync<UserDto>("users", request);
    public async Task<bool> UpdateUserAsync(int id, UpdateUserRequest request) => await PutAsync($"users/{id}", request);
    public async Task<bool> DeleteUserAsync(int id) => await DeleteAsync($"users/{id}");

    // Game Stations
    public async Task<List<GameStationDto>?> GetGameStationsAsync() => await GetAsync<List<GameStationDto>>("stations");
    public async Task<GameStationDto?> GetGameStationAsync(int id) => await GetAsync<GameStationDto>($"stations/{id}");
    public async Task<GameStationDto?> CreateGameStationAsync(GameStationDto station) => await PostAsync<GameStationDto>("stations", station);
    public async Task<bool> UpdateGameStationAsync(int id, GameStationDto station) => await PutAsync($"stations/{id}", station);
    public async Task<bool> DeleteGameStationAsync(int id) => await DeleteAsync($"stations/{id}");

    // Game Sessions
    public async Task<List<GameSessionDto>?> GetSessionsAsync() => await GetAsync<List<GameSessionDto>>("gamesessions");
    public async Task<GameSessionDto?> GetSessionAsync(int id) => await GetAsync<GameSessionDto>($"gamesessions/{id}");
    public async Task<bool> EndSessionAsync(int id) => await PostAsync<bool>($"gamesessions/{id}/end", new { });

    // Products
    public async Task<List<ProductDto>?> GetProductsAsync() => await GetAsync<List<ProductDto>>("products");
    public async Task<ProductDto?> GetProductAsync(int id) => await GetAsync<ProductDto>($"products/{id}");
    public async Task<ProductDto?> CreateProductAsync(CreateProductRequest request) => await PostAsync<ProductDto>("products", request);
    public async Task<bool> UpdateProductAsync(int id, UpdateProductRequest request) => await PutAsync($"products/{id}", request);
    public async Task<bool> DeleteProductAsync(int id) => await DeleteAsync($"products/{id}");

    // Inventory
    public async Task<List<InventoryMovementDto>?> GetInventoryMovementsAsync() => await GetAsync<List<InventoryMovementDto>>("inventory");
    public async Task<InventoryMovementDto?> CreateInventoryMovementAsync(CreateInventoryMovementRequest request) => 
        await PostAsync<InventoryMovementDto>("inventory", request);

    // Reports
    public async Task<DashboardStatsDto?> GetDashboardStatsAsync() => await GetAsync<DashboardStatsDto>("reports/dashboard");
    public async Task<RevenueReportDto?> GetRevenueReportAsync(GetRevenueReportRequest request) => 
        await PostAsync<RevenueReportDto>("reports/revenue", request);
    public async Task<UsageReportDto?> GetUsageReportAsync(GetUsageReportRequest request) => 
        await PostAsync<UsageReportDto>("reports/usage", request);

    // Reservations
    public async Task<List<ReservationDto>?> GetReservationsAsync() => await GetAsync<List<ReservationDto>>("reservations");
    public async Task<ReservationDto?> GetReservationAsync(int id) => await GetAsync<ReservationDto>($"reservations/{id}");
    public async Task<bool> UpdateReservationAsync(int id, UpdateReservationRequest request) => 
        await PutAsync($"reservations/{id}", request);

    // Consoles
    public async Task<List<GameConsoleDto>?> GetConsolesAsync() => await GetAsync<List<GameConsoleDto>>("consoles");
    public async Task<GameConsoleDto?> GetConsoleAsync(int id) => await GetAsync<GameConsoleDto>($"consoles/{id}");
    public async Task<GameConsoleDto?> CreateConsoleAsync(CreateConsoleRequest request) => 
        await PostAsync<GameConsoleDto>("consoles", request);
    public async Task<bool> UpdateConsoleAsync(int id, UpdateConsoleRequest request) => 
        await PutAsync($"consoles/{id}", request);
}
