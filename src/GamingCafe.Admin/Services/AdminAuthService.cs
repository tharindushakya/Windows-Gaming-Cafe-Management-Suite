using System.Security.Claims;
using System.Text;
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

                // Return the deserialized response even when an access token is not yet present
                // (server may require two-factor and return RequiresTwoFactor=true with no token).
                if (loginResponse != null)
                {
                    // Only create the authentication cookie when an access token is issued
                    if (!string.IsNullOrEmpty(loginResponse.AccessToken))
                    {
                        await StoreTokenAsync(loginResponse.AccessToken);
                        await CreateAuthenticationCookieAsync(loginResponse.User, loginResponse.AccessToken);
                    }

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

    public Task<string?> GetTokenAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var token = httpContext.User.FindFirst("AccessToken")?.Value;
            return Task.FromResult<string?>(token);
        }
        return Task.FromResult<string?>(null);
    }

    public Task<UserDto?> GetCurrentUserAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userJson = httpContext.User.FindFirst("UserData")?.Value;
            if (!string.IsNullOrEmpty(userJson))
            {
                var user = JsonSerializer.Deserialize<UserDto>(userJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return Task.FromResult(user);
            }
        }
        return Task.FromResult<UserDto?>(null);
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

    private Task StoreTokenAsync(string token)
    {
        // Token will be stored in the authentication cookie
        // This is handled in CreateAuthenticationCookieAsync
        return Task.CompletedTask;
    }

    private Task ClearTokenAsync()
    {
        // Token is cleared when the authentication cookie is removed
        return Task.CompletedTask;
    }

    private async Task CreateAuthenticationCookieAsync(UserDto user, string? accessToken = null)
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

        // Include the access token in the claims if provided
        if (!string.IsNullOrEmpty(accessToken))
        {
            claims.Add(new Claim("AccessToken", accessToken));
        }

        var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        await httpContext.SignInAsync("Cookies", claimsPrincipal);
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(new { Email = email }), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/auth/forgot-password", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Forgot Password error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/auth/register", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Register error: {ex.Message}");
            return false;
        }
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

                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                return new AuthenticationState(claimsPrincipal);
            }
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }
}
