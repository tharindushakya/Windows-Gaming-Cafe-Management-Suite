using OtpNet;
using QRCoder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Core.DTOs;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace GamingCafe.Data.Services;

/// <summary>
/// Service for handling two-factor authentication using TOTP (Time-based One-Time Password)
/// </summary>
public class TwoFactorService : ITwoFactorService
{
    private readonly GamingCafeContext _context;
    private readonly ILogger<TwoFactorService> _logger;
    private readonly Microsoft.AspNetCore.DataProtection.IDataProtector _protector;

    public TwoFactorService(GamingCafeContext context, ILogger<TwoFactorService> logger, IDataProtectionProvider provider)
    {
        _context = context;
        _logger = logger;
        // Use injected data protection provider to create a protector for 2FA secrets
        _protector = provider.CreateProtector("TwoFactorSecrets.v1");
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
            // Protect the secret before storing in DB
            user.TwoFactorSecretKey = _protector.Protect(secretKey);
            // Hash recovery codes (PBKDF2 with per-code salt) and store serialized list
            var hashedRecovery = GenerateHashedRecoveryCodes(recoveryCodes);
            user.TwoFactorRecoveryCode = string.Join(",", hashedRecovery);
            user.IsTwoFactorEnabled = false; // User needs to verify setup first

            // Mark that a setup is pending by setting a short-lived token
            user.EmailVerificationToken = GenerateSecureToken();
            user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddMinutes(10);

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

            // Unprotect secret before verifying
            var secret = _protector.Unprotect(user.TwoFactorSecretKey);
            return VerifyCode(secret, code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying two-factor code for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Confirm and enable 2FA after initial setup by verifying a code.
    /// </summary>
    public async Task<bool> ConfirmSetupAsync(int userId, string code)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.TwoFactorSecretKey))
                return false;

            var secret = _protector.Unprotect(user.TwoFactorSecretKey);
            var verified = VerifyCode(secret, code);
            if (!verified)
                return false;

            user.IsTwoFactorEnabled = true;
            // Clear setup token
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming two-factor setup for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Verify a recovery code for a user
    /// </summary>
    public async Task<bool> VerifyRecoveryCodeAsync(int userId, string recoveryCode)
    {
        const int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || string.IsNullOrEmpty(user.TwoFactorRecoveryCode))
                {
                    return false;
                }

                // Stored recovery codes are hashed; verify entered code against stored hashes
                var storedList = user.TwoFactorRecoveryCode.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                if (ValidateHashedBackupCodeAndConsume(storedList, recoveryCode, out var updatedList))
                {
                    // Remove the used recovery code
                    user.TwoFactorRecoveryCode = string.Join(",", updatedList);
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict when verifying recovery code for user {UserId}, attempt {Attempt}", userId, attempt + 1);
                // On concurrency conflict, retry after reloading the entry
                foreach (var entry in ex.Entries)
                {
                    if (entry.Entity is GamingCafe.Core.Models.User)
                    {
                        await entry.ReloadAsync();
                    }
                }
                // retry
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying recovery code for user {UserId}", userId);
                return false;
            }
        }

        _logger.LogWarning("Max retry attempts reached when verifying recovery code for user {UserId}", userId);
        return false;
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
            // Store hashed recovery codes (PBKDF2 per-code salt) so they are one-way and consumable
            var hashed = GenerateHashedRecoveryCodes(newRecoveryCodes);

            const int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    user.TwoFactorRecoveryCode = string.Join(",", hashed);
                    await _context.SaveChangesAsync();
                    break;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, "Concurrency conflict when generating new recovery codes for user {UserId}, attempt {Attempt}", userId, attempt + 1);
                    // reload and retry
                    foreach (var entry in ex.Entries)
                    {
                        if (entry.Entity is GamingCafe.Core.Models.User)
                        {
                            await entry.ReloadAsync();
                        }
                    }
                }
            }

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
        // Use cryptographic RNG to generate recovery codes
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var charArray = chars.ToCharArray();
        var charCount = charArray.Length;

        for (int i = 0; i < count; i++)
        {
            var buffer = new byte[8]; // 8 chars
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(buffer);

            var codeChars = new char[8];
            for (int j = 0; j < 8; j++)
            {
                // Use the random byte to index into the character set
                var idx = buffer[j] % charCount;
                codeChars[j] = charArray[idx];
            }

            var code = new string(codeChars);
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

    // Hash recovery codes using PBKDF2 with per-code salt. Each entry format: salt:hash (both base64)
    private List<string> GenerateHashedRecoveryCodes(List<string> codes)
    {
        var list = new List<string>();
        foreach (var code in codes)
        {
            var salt = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            var normalized = code?.Replace("-", "").Replace(" ", "").ToUpperInvariant() ?? string.Empty;
            var hash = Pbkdf2Hash(normalized, salt);
            list.Add(Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash));
        }
        return list;
    }

    private byte[] Pbkdf2Hash(string input, byte[] salt, int iterations = 100_000, int length = 32)
    {
        using var derive = new Rfc2898DeriveBytes(input, salt, iterations, HashAlgorithmName.SHA256);
        return derive.GetBytes(length);
    }

    // Verifies entered code against stored hashed list. If match, consumes it and returns updated list.
    private bool ValidateHashedBackupCodeAndConsume(List<string> storedList, string enteredCode, out List<string> updated)
    {
        updated = new List<string>(storedList);
        var normalized = enteredCode?.Replace("-", "").Replace(" ", "").ToUpperInvariant() ?? string.Empty;

        for (int i = 0; i < storedList.Count; i++)
        {
            var parts = storedList[i].Split(':');
            if (parts.Length != 2) continue;
            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = Convert.FromBase64String(parts[1]);
            var candidateHash = Pbkdf2Hash(normalized, salt);
            if (CryptographicOperations.FixedTimeEquals(candidateHash, storedHash))
            {
                // consume
                updated.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    #endregion

    private string GenerateSecureToken()
    {
        var randomNumber = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber).Replace("+", "").Replace("/", "").Replace("=", "");
    }
}
