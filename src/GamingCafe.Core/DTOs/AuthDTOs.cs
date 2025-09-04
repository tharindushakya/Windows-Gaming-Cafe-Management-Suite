namespace GamingCafe.Core.DTOs;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? TwoFactorCode { get; set; }
    public string? RecoveryCode { get; set; }
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
    public bool RequiresTwoFactor { get; set; }
    public string? TwoFactorToken { get; set; }
}

public class RefreshTokenRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class PasswordResetRequest
{
    public string Email { get; set; } = string.Empty;
}

public class PasswordResetConfirmRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class EmailVerificationRequest
{
    public string Email { get; set; } = string.Empty;
}

public class EmailVerificationConfirmRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class TwoFactorSetupRequest
{
    public string Password { get; set; } = string.Empty;
}

public class TwoFactorSetupResponse
{
    public string SecretKey { get; set; } = string.Empty;
    public string QrCodeDataUrl { get; set; } = string.Empty;
    public List<string> RecoveryCodes { get; set; } = new();
}

public class TwoFactorVerifyRequest
{
    public string Code { get; set; } = string.Empty;
    public string? RecoveryCode { get; set; }
}

public class TwoFactorDisableRequest
{
    public string Password { get; set; } = string.Empty;
}

public class TwoFactorRecoveryCodesResponse
{
    public List<string> RecoveryCodes { get; set; } = new();
}

public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public decimal WalletBalance { get; set; }
    public int LoyaltyPoints { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
}
