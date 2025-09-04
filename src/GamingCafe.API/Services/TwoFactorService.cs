using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Core.DTOs;
using GamingCafe.Core.Models;
using GamingCafe.Data;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using QRCoder;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;

namespace GamingCafe.API.Services;

public class TwoFactorService : ITwoFactorService
{
    private readonly GamingCafeContext _context;
    private readonly ILogger<TwoFactorService> _logger;

    public TwoFactorService(GamingCafeContext context, ILogger<TwoFactorService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TwoFactorSetupResponse> SetupTwoFactorAsync(int userId, string password)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                throw new ArgumentException("User not found");

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid password");

            // Generate secret key
            var secretKey = KeyGeneration.GenerateRandomKey(20);
            var base32Secret = Base32Encoding.ToString(secretKey);

            // Generate recovery codes
            var recoveryCodes = GenerateRecoveryCodes(10);
            var hashedRecoveryCodes = recoveryCodes.Select(code => BCrypt.Net.BCrypt.HashPassword(code)).ToList();

            // Update user
            user.TwoFactorSecretKey = base32Secret;
            user.TwoFactorRecoveryCode = string.Join(",", hashedRecoveryCodes);
            user.IsTwoFactorEnabled = true;

            await _context.SaveChangesAsync();

            // Generate QR code
            var qrCodeDataUrl = GenerateQrCodeDataUrl(user.Email, base32Secret);

            _logger.LogInformation("Two-factor authentication setup for user {UserId}", userId);

            return new TwoFactorSetupResponse
            {
                SecretKey = base32Secret,
                QrCodeDataUrl = qrCodeDataUrl,
                RecoveryCodes = recoveryCodes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up two-factor authentication for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> VerifyTwoFactorAsync(int userId, string code)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null || !user.IsTwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecretKey))
                return false;

            var secretKey = Base32Encoding.ToBytes(user.TwoFactorSecretKey);
            var totp = new Totp(secretKey);
            
            // Check current code with some tolerance for clock skew
            var isValid = totp.VerifyTotp(code, out long timeStepMatched, window: VerificationWindow.RfcSpecifiedNetworkDelay);
            
            if (isValid)
            {
                _logger.LogInformation("Two-factor authentication successful for user {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Two-factor authentication failed for user {UserId}", userId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying two-factor code for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> VerifyRecoveryCodeAsync(int userId, string recoveryCode)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null || !user.IsTwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorRecoveryCode))
                return false;

            var hashedRecoveryCodes = user.TwoFactorRecoveryCode.Split(',').ToList();
            var matchingCodeIndex = -1;

            for (int i = 0; i < hashedRecoveryCodes.Count; i++)
            {
                if (BCrypt.Net.BCrypt.Verify(recoveryCode, hashedRecoveryCodes[i]))
                {
                    matchingCodeIndex = i;
                    break;
                }
            }

            if (matchingCodeIndex >= 0)
            {
                // Remove the used recovery code
                hashedRecoveryCodes.RemoveAt(matchingCodeIndex);
                user.TwoFactorRecoveryCode = string.Join(",", hashedRecoveryCodes);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Recovery code used successfully for user {UserId}", userId);
                return true;
            }

            _logger.LogWarning("Invalid recovery code for user {UserId}", userId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying recovery code for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> DisableTwoFactorAsync(int userId, string password)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return false;

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid password");

            user.IsTwoFactorEnabled = false;
            user.TwoFactorSecretKey = null;
            user.TwoFactorRecoveryCode = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Two-factor authentication disabled for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling two-factor authentication for user {UserId}", userId);
            throw;
        }
    }

    public async Task<TwoFactorRecoveryCodesResponse> GenerateNewRecoveryCodesAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null || !user.IsTwoFactorEnabled)
                throw new ArgumentException("User not found or 2FA not enabled");

            var newRecoveryCodes = GenerateRecoveryCodes(10);
            var hashedRecoveryCodes = newRecoveryCodes.Select(code => BCrypt.Net.BCrypt.HashPassword(code)).ToList();

            user.TwoFactorRecoveryCode = string.Join(",", hashedRecoveryCodes);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New recovery codes generated for user {UserId}", userId);

            return new TwoFactorRecoveryCodesResponse
            {
                RecoveryCodes = newRecoveryCodes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating new recovery codes for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> IsTwoFactorEnabledAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        return user?.IsTwoFactorEnabled ?? false;
    }

    public string GenerateQrCodeDataUrl(string email, string secretKey)
    {
        try
        {
            var issuer = "Gaming Cafe Management";
            var label = $"{issuer}:{email}";
            var provisioningUri = $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secretKey}&issuer={Uri.EscapeDataString(issuer)}";

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(provisioningUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            
            var qrCodeBytes = qrCode.GetGraphic(20);
            var base64String = Convert.ToBase64String(qrCodeBytes);
            
            return $"data:image/png;base64,{base64String}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code for email {Email}", email);
            throw;
        }
    }

    private static List<string> GenerateRecoveryCodes(int count)
    {
        var codes = new List<string>();
        using var rng = RandomNumberGenerator.Create();
        
        for (int i = 0; i < count; i++)
        {
            var bytes = new byte[5]; // 5 bytes = 10 characters in hex
            rng.GetBytes(bytes);
            var code = Convert.ToHexString(bytes).ToLower();
            codes.Add($"{code[..5]}-{code[5..]}"); // Format as XXXXX-XXXXX
        }
        
        return codes;
    }
}
