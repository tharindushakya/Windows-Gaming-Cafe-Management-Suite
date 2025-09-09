using GamingCafe.Core.Models;
using GamingCafe.Core.DTOs;
using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Core.Interfaces.Background;
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
    private readonly IBackgroundTaskQueue? _taskQueue;
    private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
    private readonly Microsoft.Extensions.Caching.Distributed.IDistributedCache? _distributedCache;
    private readonly TimeSpan _twoFactorTtl;

    public AuthService(GamingCafeContext context, IConfiguration configuration, IServiceProvider serviceProvider, Microsoft.Extensions.Caching.Memory.IMemoryCache cache, Microsoft.Extensions.Caching.Distributed.IDistributedCache? distributedCache = null, IBackgroundTaskQueue? taskQueue = null)
    {
        _context = context;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _cache = cache;
        _distributedCache = distributedCache;
        _taskQueue = taskQueue;
        // TTL for transient 2FA tokens; config key: Auth:TwoFactor:TransientTtlMinutes
        var ttlMinutes = _configuration.GetValue<int?>("Auth:TwoFactor:TransientTtlMinutes") ?? 5;
        _twoFactorTtl = TimeSpan.FromMinutes(ttlMinutes);
    }

    private async Task SetTwoFactorTokenAsync(string token, int userId, TimeSpan ttl)
    {
    var key = $"auth:2fa:{token}";
        if (_distributedCache != null)
        {
            var bytes = BitConverter.GetBytes(userId);
            var options = new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };
            await _distributedCache.SetAsync(key, bytes, options);
            return;
        }

        // fallback to memory cache
        using (var entry = _cache.CreateEntry(key))
        {
            entry.Value = userId;
            entry.AbsoluteExpirationRelativeToNow = ttl;
        }
    }

    private async Task<int?> TryGetTwoFactorUserIdAsync(string token)
    {
    var key = $"auth:2fa:{token}";
        if (_distributedCache != null)
        {
            try
            {
                var bytes = await _distributedCache.GetAsync(key);
                if (bytes == null || bytes.Length == 0) return null;
                return BitConverter.ToInt32(bytes, 0);
            }
            catch
            {
                return null;
            }
        }

        if (_cache.TryGetValue(key, out var obj) && obj is int id)
            return id;

        return null;
    }

    private async Task RemoveTwoFactorTokenAsync(string token)
    {
    var key = $"auth:2fa:{token}";
        if (_distributedCache != null)
        {
            await _distributedCache.RemoveAsync(key);
            return;
        }

        _cache.Remove(key);
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
                // Prefer distributed cache (Redis) for multi-instance resilience; fall back to in-memory cache.
                await SetTwoFactorTokenAsync(twoFactorToken, user.UserId, _twoFactorTtl);

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
                            CreatedAt = user.CreatedAt,
                            Role = user.Role,
                            WalletBalance = (await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == user.UserId))?.Balance ?? 0m,
                            LoyaltyPoints = user.LoyaltyPoints,
                            IsTwoFactorEnabled = user.IsTwoFactorEnabled
                        }
                };
            }

            // When a code is provided, require a valid transient two-factor token as proof this flow was initiated
            int? cachedUserId = null;
            if (string.IsNullOrEmpty(request.TwoFactorToken) || (cachedUserId = await TryGetTwoFactorUserIdAsync(request.TwoFactorToken)) == null || cachedUserId != user.UserId)
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
            await RemoveTwoFactorTokenAsync(request.TwoFactorToken!);
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
                CreatedAt = user.CreatedAt,
                Role = user.Role,
                WalletBalance = (await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == user.UserId))?.Balance ?? 0m,
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

        // Ensure token belongs to this user
        var tokenEntity = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == incomingHash && t.UserId == user.UserId);
        if (tokenEntity == null || tokenEntity.RevokedAt != null || tokenEntity.ExpiresAt <= DateTime.UtcNow)
        {
            // Possible token reuse or invalid token. Revoke all user's tokens as a precaution.
            var userTokens = _context.RefreshTokens.Where(t => t.UserId == user.UserId && t.RevokedAt == null);
            await userTokens.ForEachAsync(t => t.RevokedAt = DateTime.UtcNow);
            await _context.SaveChangesAsync();

            // Emit metric for reuse detection
            try { GamingCafe.Core.Observability.RefreshTokenReuseCounter.Add(1); } catch { }

            // Audit the reuse detection if audit service is available
            try
            {
                var audit = _serviceProvider.GetService<GamingCafe.Core.Interfaces.Services.IAuditService>();
                if (audit != null)
                {
                    await audit.LogActionAsync("RefreshTokenReuseDetected", user.UserId, System.Text.Json.JsonSerializer.Serialize(new { TokenHash = incomingHash, Ip = request.IpAddress, DeviceInfo = request.DeviceInfo }));
                }
                else
                {
                    var logger = _serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger<AuthService>>();
                    logger?.LogWarning("Refresh token reuse detected for user {UserId}", user.UserId);
                }
            }
            catch { }

            return null;
        }

        // Perform atomic rotation using a transaction and a conditional update to avoid race conditions
        var newRawToken = GenerateRefreshToken();
        var newHash = ComputeHash(newRawToken);
        var newTokenId = Guid.NewGuid();

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // Conditional update: only revoke the token if it is still active (RevokedAt IS NULL)
            var rows = await _context.Database.ExecuteSqlRawAsync(
                "UPDATE \"RefreshTokens\" SET \"RevokedAt\" = now(), \"ReplacedByTokenId\" = {0} WHERE \"TokenId\" = {1} AND \"RevokedAt\" IS NULL;",
                newTokenId, tokenEntity.TokenId);

            if (rows != 1)
            {
                // Someone else used/revoked the token concurrently: treat as reuse
                await tx.RollbackAsync();

                var userTokens = _context.RefreshTokens.Where(t => t.UserId == user.UserId && t.RevokedAt == null);
                await userTokens.ForEachAsync(t => t.RevokedAt = DateTime.UtcNow);
                await _context.SaveChangesAsync();

                // Emit metric for reuse detection
                try { GamingCafe.Core.Observability.RefreshTokenReuseCounter.Add(1); } catch { }

                try
                {
                    var audit = _serviceProvider.GetService<GamingCafe.Core.Interfaces.Services.IAuditService>();
                    if (audit != null)
                    {
                        await audit.LogActionAsync("RefreshTokenReuseDetected", user.UserId, System.Text.Json.JsonSerializer.Serialize(new { TokenHash = incomingHash, Ip = request.IpAddress, DeviceInfo = request.DeviceInfo }));
                    }
                }
                catch { }

                return null;
            }

            // Insert new token row
            var insertSql = "INSERT INTO \"RefreshTokens\" (\"TokenId\", \"UserId\", \"TokenHash\", \"DeviceInfo\", \"IpAddress\", \"CreatedAt\", \"ExpiresAt\") VALUES ({0}, {1}, {2}, {3}, {4}, now(), {5});";
            await _context.Database.ExecuteSqlRawAsync(insertSql, newTokenId, user.UserId, newHash, (object?)request.DeviceInfo ?? DBNull.Value, (object?)request.IpAddress ?? DBNull.Value, DateTime.UtcNow.AddDays(7));

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        // Return new access + refresh token
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

        // Send welcome email and initiate email verification using background queue
        try
        {
            var emailService = _serviceProvider.GetService<IEmailService>();
            if (emailService != null)
            {
                if (_taskQueue != null)
                {
                    _taskQueue.QueueBackgroundWorkItem(async ct => await emailService.SendWelcomeEmailAsync(user.Email, user.Username));
                }
                else
                {
                    _ = emailService.SendWelcomeEmailAsync(user.Email, user.Username).ContinueWith(t =>
                    {
                        if (t.Exception != null)
                        {
                            var logger = _serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger<AuthService>>();
                            logger?.LogError(t.Exception, "SendWelcomeEmailAsync background invocation failed");
                        }
                    }, TaskScheduler.Default);
                }
            }

            // Initiate email verification (stores token and sends verification email)
            if (_taskQueue != null)
            {
                _taskQueue.QueueBackgroundWorkItem(async ct => await InitiateEmailVerificationAsync(user.Email));
            }
            else
            {
                _ = InitiateEmailVerificationAsync(user.Email).ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        var logger = _serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger<AuthService>>();
                        logger?.LogError(t.Exception, "InitiateEmailVerificationAsync background invocation failed");
                    }
                }, TaskScheduler.Default);
            }
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
