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
using Microsoft.Extensions.Caching.Memory;

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
    private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

    public AuthService(GamingCafeContext context, IConfiguration configuration, IServiceProvider serviceProvider, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
    {
        _context = context;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _cache = cache;
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
                // Store the 2FA token in an in-memory cache with a short TTL
                // Fallback to CreateEntry to avoid extension method resolution issues
                var cacheKey = $"2fa:{twoFactorToken}";
                using (var entry = _cache.CreateEntry(cacheKey))
                {
                    entry.Value = user.UserId;
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                }

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
                        Role = user.Role,
                        WalletBalance = user.WalletBalance,
                        LoyaltyPoints = user.LoyaltyPoints,
                        IsTwoFactorEnabled = user.IsTwoFactorEnabled
                    }
                };
            }

            // When a code is provided, require a valid transient two-factor token as proof this flow was initiated
            object? cachedObj;
            if (string.IsNullOrEmpty(request.TwoFactorToken) || !_cache.TryGetValue($"2fa:{request.TwoFactorToken}", out cachedObj) || !(cachedObj is int cachedUserId) || cachedUserId != user.UserId)
            {
                // Token missing/expired or mismatch
                return null;
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

            // Remove transient token after successful verification
            _cache.Remove($"2fa:{request.TwoFactorToken}");
        }

        var accessToken = GenerateJwtToken(user);
        var rawRefreshToken = GenerateRefreshToken();

        // Persist only a hash of the refresh token
        var refreshTokenHash = ComputeHash(rawRefreshToken);

        var refreshTokenEntity = new GamingCafe.Core.Models.RefreshToken
        {
            UserId = user.UserId,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IpAddress = request.IpAddress ?? null,
            DeviceInfo = request.DeviceInfo ?? null
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        user.LastLoginAt = DateTime.UtcNow;
        // Clear temporary 2FA token
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;

    await _context.SaveChangesAsync();

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15), // JWT expires in 15 minutes
            RequiresTwoFactor = false,
                User = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
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

        var user = await _context.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            return null;

        var incomingHash = ComputeHash(request.RefreshToken);

        var tokenEntity = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == incomingHash);
        if (tokenEntity == null || tokenEntity.RevokedAt != null || tokenEntity.ExpiresAt <= DateTime.UtcNow)
        {
            // Possible token reuse or invalid token. Revoke all user's tokens as a precaution.
            var userTokens = _context.RefreshTokens.Where(t => t.UserId == user.UserId && t.RevokedAt == null);
            await userTokens.ForEachAsync(t => t.RevokedAt = DateTime.UtcNow);
            await _context.SaveChangesAsync();
            return null;
        }

        // Rotation: create a new token and mark the old as replaced/revoked
        var newRawToken = GenerateRefreshToken();
        var newHash = ComputeHash(newRawToken);

        var newTokenEntity = new GamingCafe.Core.Models.RefreshToken
        {
            UserId = user.UserId,
            TokenHash = newHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IpAddress = request.IpAddress ?? tokenEntity.IpAddress,
            DeviceInfo = request.DeviceInfo ?? tokenEntity.DeviceInfo
        };

        tokenEntity.RevokedAt = DateTime.UtcNow;
        tokenEntity.ReplacedByTokenId = newTokenEntity.TokenId;

        _context.RefreshTokens.Add(newTokenEntity);
        await _context.SaveChangesAsync();

        var newAccessToken = GenerateJwtToken(user);

        return new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRawToken,
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

        // Send password reset email
        try
        {
            var emailService = _serviceProvider.GetService<IEmailService>();
            if (emailService != null)
            {
                await emailService.SendPasswordResetEmailAsync(email, resetToken);
            }
        }
        catch (Exception)
        {
            // Log later if necessary; we don't block reset for email failures
        }

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

    // Invalidate all existing refresh tokens for the user
    var userTokens = _context.RefreshTokens.Where(t => t.UserId == user.UserId && t.RevokedAt == null);
    await userTokens.ForEachAsync(t => t.RevokedAt = DateTime.UtcNow);

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
        var hash = ComputeHash(refreshToken);
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (token == null)
            return false;

        token.RevokedAt = DateTime.UtcNow;
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

        // Send welcome email and initiate email verification
        try
        {
            var emailService = _serviceProvider.GetService<IEmailService>();
            if (emailService != null)
            {
                _ = Task.Run(async () => await emailService.SendWelcomeEmailAsync(user.Email, user.Username));
            }

            // Initiate email verification (stores token and sends verification email)
            _ = Task.Run(async () => await InitiateEmailVerificationAsync(user.Email));
        }
        catch
        {
            // Non-fatal: registration succeeded even if emails fail
        }

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

    private string ComputeHash(string token)
    {
        var key = _configuration["RefreshToken:HashKey"] ?? _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(key))
            throw new InvalidOperationException("Refresh token hash key is not configured");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = hmac.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
