using GamingCafe.Core.Models;
using GamingCafe.Core.DTOs;
using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace GamingCafe.API.Services;

public interface IAuthService
{
    Task<LoginResponse?> AuthenticateAsync(LoginRequest request);
    Task<RefreshTokenResponse?> RefreshTokenAsync(RefreshTokenRequest request);
    Task<User?> RegisterAsync(User user, string password);
    Task<bool> InitiatePasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(PasswordResetConfirmRequest request);
    Task<bool> InitiateEmailVerificationAsync(string email);
    Task<bool> VerifyEmailAsync(EmailVerificationConfirmRequest request);
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken);
    
    // Legacy method for backward compatibility
    Task<string?> AuthenticateAsync(string username, string password);
}

public class AuthService : IAuthService
{
    private readonly GamingCafeContext _context;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public AuthService(GamingCafeContext context, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _context = context;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public async Task<LoginResponse?> AuthenticateAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        // Check if user has 2FA enabled
        if (user.IsTwoFactorEnabled)
        {
            // If no 2FA code provided, return response indicating 2FA required
            if (string.IsNullOrEmpty(request.TwoFactorCode) && string.IsNullOrEmpty(request.RecoveryCode))
            {
                var twoFactorToken = GenerateSecureToken();
                // Store the 2FA token temporarily (you might want to use cache or database for this)
                user.EmailVerificationToken = twoFactorToken; // Reusing this field for simplicity
                user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddMinutes(5); // 5 minutes to complete 2FA
                await _context.SaveChangesAsync();

                return new LoginResponse
                {
                    RequiresTwoFactor = true,
                    TwoFactorToken = twoFactorToken,
                    User = new UserDto
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Role = user.Role.ToString(),
                        WalletBalance = user.WalletBalance,
                        LoyaltyPoints = user.LoyaltyPoints,
                        IsTwoFactorEnabled = user.IsTwoFactorEnabled
                    }
                };
            }

            // Verify 2FA code or recovery code
            bool isValidTwoFactor = false;
            if (!string.IsNullOrEmpty(request.TwoFactorCode))
            {
                var twoFactorService = _serviceProvider.GetRequiredService<ITwoFactorService>();
                isValidTwoFactor = await twoFactorService.VerifyTwoFactorAsync(user.UserId, request.TwoFactorCode);
            }
            else if (!string.IsNullOrEmpty(request.RecoveryCode))
            {
                var twoFactorService = _serviceProvider.GetRequiredService<ITwoFactorService>();
                isValidTwoFactor = await twoFactorService.VerifyRecoveryCodeAsync(user.UserId, request.RecoveryCode);
            }

            if (!isValidTwoFactor)
                return null;
        }

        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // 7 days
        user.LastLoginAt = DateTime.UtcNow;
        // Clear temporary 2FA token
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;

        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15), // JWT expires in 15 minutes
            RequiresTwoFactor = false,
            User = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString(),
                WalletBalance = user.WalletBalance,
                LoyaltyPoints = user.LoyaltyPoints,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled
            }
        };
    }

    public async Task<RefreshTokenResponse?> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
            return null;

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
            return null;

        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.RefreshToken != request.RefreshToken || 
            user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return null;

        var newAccessToken = GenerateJwtToken(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync();

        return new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
    }

    public async Task<bool> InitiatePasswordResetAsync(string email)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null)
            return false; // Don't reveal if email exists

        var resetToken = GeneratePasswordResetToken();
        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // 1 hour expiry

        await _context.SaveChangesAsync();

        // TODO: Send email with reset token
        // await _emailService.SendPasswordResetEmailAsync(email, resetToken);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(PasswordResetConfirmRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && 
                                    u.PasswordResetToken == request.Token &&
                                    u.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.RefreshToken = null; // Invalidate all refresh tokens

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> InitiateEmailVerificationAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || user.IsEmailVerified)
            return false;

        var verificationToken = GenerateSecureToken();
        user.EmailVerificationToken = verificationToken;
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);

        await _context.SaveChangesAsync();

        // Send verification email
        var emailService = _serviceProvider.GetRequiredService<IEmailService>();
        await emailService.SendEmailVerificationAsync(user.Email, user.Username, verificationToken);

        return true;
    }

    public async Task<bool> VerifyEmailAsync(EmailVerificationConfirmRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && 
                                    u.EmailVerificationToken == request.Token &&
                                    u.EmailVerificationTokenExpiry > DateTime.UtcNow);

        if (user == null || user.IsEmailVerified)
            return false;

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        if (user == null)
            return false;

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        await _context.SaveChangesAsync();
        return true;
    }

    // Legacy method for backward compatibility
    public async Task<string?> AuthenticateAsync(string username, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return GenerateJwtToken(user);
    }

    public async Task<User?> RegisterAsync(User user, string password)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == user.Username || u.Email == user.Email);

        if (existingUser != null)
            return null;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"];
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];

        if (string.IsNullOrEmpty(jwtKey))
            throw new InvalidOperationException("JWT Key is not configured");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15), // Short-lived access token
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private string GenerateSecureToken()
    {
        var randomNumber = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber).Replace("+", "").Replace("/", "").Replace("=", "");
    }

    private string GeneratePasswordResetToken()
    {
        var randomNumber = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber).Replace("+", "").Replace("/", "").Replace("=", "");
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var jwtKey = _configuration["Jwt:Key"];
        
        if (string.IsNullOrEmpty(jwtKey))
            throw new InvalidOperationException("JWT Key is not configured");
            
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = false // We want to check expired tokens
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
            
            if (validatedToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
