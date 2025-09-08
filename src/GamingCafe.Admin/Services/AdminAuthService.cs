using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using GamingCafe.Core.DTOs;

namespace GamingCafe.Admin.Services;

public class AdminAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private string? _baseUrl;

    public AdminAuthService(
        HttpClient httpClient, 
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001";
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/auth/login", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.AccessToken))
                {
                    await StoreTokenAsync(loginResponse.AccessToken);
                    await CreateAuthenticationCookieAsync(loginResponse.User);
                    return loginResponse;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
        }

        return null;
    }

    public async Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await httpContext.SignOutAsync("Cookies");
        }
        
        // Clear stored token
        await ClearTokenAsync();
    }

    public async Task<string?> GetTokenAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            return httpContext.User.FindFirst("AccessToken")?.Value;
        }
        return null;
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userJson = httpContext.User.FindFirst("UserData")?.Value;
            if (!string.IsNullOrEmpty(userJson))
            {
                return JsonSerializer.Deserialize<UserDto>(userJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }
        return null;
    }

    public bool IsAuthenticated()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.Identity?.IsAuthenticated == true;
    }

    public bool IsInRole(string role)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.IsInRole(role) == true;
    }

    private async Task StoreTokenAsync(string token)
    {
        // Token will be stored in the authentication cookie
        // This is handled in CreateAuthenticationCookieAsync
        await Task.CompletedTask;
    }

    private async Task ClearTokenAsync()
    {
        // Token is cleared when the authentication cookie is removed
        await Task.CompletedTask;
    }

    private async Task CreateAuthenticationCookieAsync(UserDto user)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName),
            new Claim("UserData", JsonSerializer.Serialize(user))
        };

        var token = await GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            claims.Add(new Claim("AccessToken", token));
        }

        var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        await httpContext.SignInAsync("Cookies", claimsPrincipal);
    }
}

public class AdminAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly AdminAuthService _authService;

    public AdminAuthenticationStateProvider(AdminAuthService authService)
    {
        _authService = authService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_authService.IsAuthenticated())
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user != null)
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                };

                var identity = new ClaimsIdentity(claims, "Cookies");
                var principal = new ClaimsPrincipal(identity);
                return new AuthenticationState(principal);
            }
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }
}
