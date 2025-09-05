using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GamingCafe.Core.DTOs;
using GamingCafe.Core.Models;

namespace GamingCafe.Admin.Services;

public class AdminApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AdminApiService> _logger;
    private string? _accessToken;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AdminApiService(IHttpClientFactory httpClientFactory, ILogger<AdminApiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("API");
        _logger = logger;
    }

    public async Task<bool> AuthenticateAsync(string email, string password)
    {
    _logger.LogDebug("AdminApiService.AuthenticateAsync called for {Email}", email);
        try
        {
            var loginRequest = new LoginRequest
            {
                Email = email,
                Password = password
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, _jsonOptions);

                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.AccessToken))
                {
                    _accessToken = loginResponse.AccessToken;
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new AuthenticationHeaderValue("Bearer", _accessToken);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during API authentication");
            return false;
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint) where T : class
    {
    _logger.LogDebug("AdminApiService.GetAsync called for endpoint {Endpoint}", endpoint);
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data from {Endpoint}", endpoint);
            return null;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest? data) 
        where TResponse : class
    {
    _logger.LogDebug("AdminApiService.PostAsync called for endpoint {Endpoint} with data type {DataType}", endpoint, typeof(TRequest).Name);
        try
        {
            if (data == null)
            {
                _logger.LogWarning("AdminApiService.PostAsync called with null data for {Endpoint}", endpoint);
                return null;
            }

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting data to {Endpoint}", endpoint);
            return null;
        }
    }

    public async Task<bool> PutAsync<T>(string endpoint, T data)
    {
    _logger.LogDebug("AdminApiService.PutAsync called for endpoint {Endpoint} with data type {DataType}", endpoint, typeof(T).Name);
        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(endpoint, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating data at {Endpoint}", endpoint);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
    _logger.LogDebug("AdminApiService.DeleteAsync called for endpoint {Endpoint}", endpoint);
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting data at {Endpoint}", endpoint);
            return false;
        }
    }

    public void Logout()
    {
    _logger.LogDebug("AdminApiService.Logout called");
        _accessToken = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

    // ----- Product helpers (thin wrappers over generic methods) -----
    public Task<List<Product>?> GetProductsAsync()
    {
        _logger.LogDebug("AdminApiService.GetProductsAsync");
        return GetAsync<List<Product>>("/api/products");
    }

    public Task<Product?> CreateProductAsync(Product product)
    {
        _logger.LogDebug("AdminApiService.CreateProductAsync -> {name}", product?.Name);
        return PostAsync<Product, Product>("/api/products", product);
    }

    public Task<bool> UpdateProductAsync(Product product)
    {
        if (product == null) return Task.FromResult(false);
        _logger.LogDebug("AdminApiService.UpdateProductAsync -> {id}", product.ProductId);
        return PutAsync($"/api/products/{product.ProductId}", product);
    }

    public Task<bool> DeleteProductAsync(int productId)
    {
        _logger.LogDebug("AdminApiService.DeleteProductAsync -> {id}", productId);
        return DeleteAsync($"/api/products/{productId}");
    }
}
