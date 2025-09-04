using OtpNet;
using QRCoder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Core.DTOs;

namespace GamingCafe.Data.Services;

/// <summary>
/// Service for handling two-factor authentication using TOTP (Time-based One-Time Password)
/// </summary>
public class TwoFactorService : ITwoFactorService
{
    private readonly GamingCafeContext _context;
    private readonly ILogger<TwoFactorService> _logger;

    public TwoFactorService(GamingCafeContext context, ILogger<TwoFactorService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Setup two-factor authentication for a user
    /// </summary>
    public async Task<TwoFactorSetupResponse> SetupTwoFactorAsync(int userId, string password)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            // Verify password (this should use your password verification logic)
            // For now, we'll assume password verification is handled elsewhere

            // Generate secret key
            var secretKey = GenerateSecretKey();
            
            // Generate QR code
            var qrCodeDataUrl = GenerateQrCodeDataUrl(user.Email, secretKey);
            
            // Generate recovery codes
            var recoveryCodes = GenerateBackupCodes();

            // Store the secret key and recovery codes in the user record
            // Note: In production, these should be encrypted
            user.TwoFactorSecretKey = secretKey;
            user.TwoFactorRecoveryCode = string.Join(",", recoveryCodes);
            user.IsTwoFactorEnabled = false; // User needs to verify setup first

            await _context.SaveChangesAsync();

            return new TwoFactorSetupResponse
            {
                SecretKey = secretKey,
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

    /// <summary>
    /// Verify a TOTP code for a user
    /// </summary>
    public async Task<bool> VerifyTwoFactorAsync(int userId, string code)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.TwoFactorSecretKey))
            {
                return false;
            }

            return VerifyCode(user.TwoFactorSecretKey, code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying two-factor code for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Verify a recovery code for a user
    /// </summary>
    public async Task<bool> VerifyRecoveryCodeAsync(int userId, string recoveryCode)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.TwoFactorRecoveryCode))
            {
                return false;
            }

            var recoveryCodes = user.TwoFactorRecoveryCode.Split(',').ToList();
            
            if (ValidateBackupCode(recoveryCodes, recoveryCode))
            {
                // Remove the used recovery code
                recoveryCodes.RemoveAll(c => c.Replace("-", "").Replace(" ", "").ToUpperInvariant() == 
                                           recoveryCode.Replace("-", "").Replace(" ", "").ToUpperInvariant());
                user.TwoFactorRecoveryCode = string.Join(",", recoveryCodes);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying recovery code for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Disable two-factor authentication for a user
    /// </summary>
    public async Task<bool> DisableTwoFactorAsync(int userId, string password)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Verify password (this should use your password verification logic)
            // For now, we'll assume password verification is handled elsewhere

            user.TwoFactorSecretKey = null;
            user.TwoFactorRecoveryCode = null;
            user.IsTwoFactorEnabled = false;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling two-factor authentication for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Generate new recovery codes for a user
    /// </summary>
    public async Task<TwoFactorRecoveryCodesResponse> GenerateNewRecoveryCodesAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            var newRecoveryCodes = GenerateBackupCodes();
            user.TwoFactorRecoveryCode = string.Join(",", newRecoveryCodes);

            await _context.SaveChangesAsync();

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

    /// <summary>
    /// Check if two-factor authentication is enabled for a user
    /// </summary>
    public async Task<bool> IsTwoFactorEnabledAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.IsTwoFactorEnabled ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking two-factor status for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Generate QR code data URL for two-factor authentication setup
    /// </summary>
    public string GenerateQrCodeDataUrl(string email, string secretKey)
    {
        var qrCodeBase64 = GenerateQrCode(email, secretKey);
        return $"data:image/png;base64,{qrCodeBase64}";
    }

    #region Private Helper Methods

    private string GenerateSecretKey()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    private string GenerateQrCode(string email, string secretKey, string issuer = "Gaming Cafe")
    {
        var totpUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={secretKey}&issuer={Uri.EscapeDataString(issuer)}";
        
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(totpUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(20);
        
        return Convert.ToBase64String(qrCodeBytes);
    }

    private bool VerifyCode(string secretKey, string code, int window = 1)
    {
        try
        {
            var keyBytes = Base32Encoding.ToBytes(secretKey);
            var totp = new Totp(keyBytes);
            
            return totp.VerifyTotp(code, out long timeStepMatched, new VerificationWindow(window, window));
        }
        catch
        {
            return false;
        }
    }

    private List<string> GenerateBackupCodes(int count = 10)
    {
        var codes = new List<string>();
        var random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            // Generate 8-character alphanumeric codes
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var code = new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            
            // Format as XXXX-XXXX for readability
            codes.Add($"{code.Substring(0, 4)}-{code.Substring(4, 4)}");
        }
        
        return codes;
    }

    private bool ValidateBackupCode(List<string> backupCodes, string enteredCode)
    {
        var normalizedCode = enteredCode?.Replace("-", "").Replace(" ", "").ToUpperInvariant();
        
        foreach (var code in backupCodes)
        {
            var normalizedBackupCode = code.Replace("-", "").Replace(" ", "").ToUpperInvariant();
            if (normalizedBackupCode == normalizedCode)
            {
                return true;
            }
        }
        
        return false;
    }

    #endregion
}
