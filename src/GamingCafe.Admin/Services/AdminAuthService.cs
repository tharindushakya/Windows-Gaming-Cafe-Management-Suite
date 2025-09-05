using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace GamingCafe.Admin.Services;

public class AdminAuthService
{
    private readonly AdminApiService _apiService;
    private readonly ILogger<AdminAuthService> _logger;

    public AdminAuthService(AdminApiService apiService, ILogger<AdminAuthService> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
    _logger.LogDebug("AdminAuthService.LoginAsync called for {Email}", email);
        try
        {
            var success = await _apiService.AuthenticateAsync(email, password);
            if (success)
            {
                _logger.LogInformation("Admin user {Email} logged in successfully", email);
                return new LoginResult { Success = true };
            }
            
            _logger.LogWarning("Failed login attempt for admin user {Email}", email);
            return new LoginResult { Success = false, ErrorMessage = "Invalid credentials" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin login for {Email}", email);
            return new LoginResult { Success = false, ErrorMessage = "Login failed due to an error" };
        }
    }

    public Task LogoutAsync()
    {
        _logger.LogDebug("AdminAuthService.LogoutAsync called");
        try
        {
            _apiService.Logout();
            _logger.LogInformation("Admin user logged out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin logout");
        }

        return Task.CompletedTask;
    }

    public bool IsAuthenticated => _apiService.IsAuthenticated;
}

public class AdminAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly AdminAuthService _authService;
    private readonly ILogger<AdminAuthenticationStateProvider> _logger;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public AdminAuthenticationStateProvider(AdminAuthService authService, ILogger<AdminAuthenticationStateProvider> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_currentUser));
    }

    public Task MarkUserAsAuthenticated(string email, string role = "Admin")
    {
    _logger?.LogDebug("AdminAuthenticationStateProvider.MarkUserAsAuthenticated called for {Email}", email);
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
        }, "AdminAuth");

        _currentUser = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));

        return Task.CompletedTask;
    }

    public Task MarkUserAsLoggedOut()
    {
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));

        return Task.CompletedTask;
    }
}

public class LoginResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
