using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using GamingCafe.API.Services;
using GamingCafe.Core.Models;
using GamingCafe.Core.DTOs;
using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Core.Interfaces;

namespace GamingCafe.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthController(IAuthService authService, IUnitOfWork unitOfWork)
    {
        _authService = authService;
        _unitOfWork = unitOfWork;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            return BadRequest("Email and password are required");

        var result = await _authService.AuthenticateAsync(request);
        if (result == null)
            return Unauthorized("Invalid credentials");

        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.AccessToken) || string.IsNullOrEmpty(request.RefreshToken))
            return BadRequest("Access token and refresh token are required");

        var result = await _authService.RefreshTokenAsync(request);
        if (result == null)
            return Unauthorized("Invalid or expired tokens");

        return Ok(result);
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
    {
        var result = await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
        if (!result)
            return BadRequest("Token revocation failed");

        return Ok(new { message = "Token revoked successfully" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] PasswordResetRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
            return BadRequest("Email is required");

        var result = await _authService.InitiatePasswordResetAsync(request.Email);
        
        // Always return success to prevent email enumeration
        return Ok(new { message = "If the email exists, a password reset link has been sent" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] PasswordResetConfirmRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || 
            string.IsNullOrEmpty(request.Token) || 
            string.IsNullOrEmpty(request.NewPassword))
            return BadRequest("Email, token, and new password are required");

        var result = await _authService.ResetPasswordAsync(request);
        if (!result)
            return BadRequest("Invalid or expired reset token");

        return Ok(new { message = "Password reset successfully" });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request == null)
            return BadRequest("Invalid request");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

    // inputs are normalized by the global NormalizeInputFilter (trim, lowercase for email/username)

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            // DateOfBirth is not collected via public registration endpoint.
            // Leave DateOfBirth as default (DateTime.MinValue) or populate later via admin profile update.
        };

        var result = await _authService.RegisterAsync(user, request.Password);
        if (result == null)
            return BadRequest("User already exists with this email or username");

        return Ok(new { message = "User registered successfully", userId = result.UserId });
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized();

        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound();

        // Read canonical wallet balance from Wallet table
        var wallet = await _unitOfWork.Repository<GamingCafe.Core.Models.Wallet>().FirstOrDefaultAsync(w => w.UserId == user.UserId);

        return Ok(new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            WalletBalance = wallet?.Balance ?? 0m,
            LoyaltyPoints = user.LoyaltyPoints
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
        }

        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("send-verification-email")]
    public async Task<IActionResult> SendVerificationEmail([FromBody] EmailVerificationRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
            return BadRequest("Email is required");

        var result = await _authService.InitiateEmailVerificationAsync(request.Email);
        if (!result)
            return BadRequest("Email not found or already verified");

        return Ok(new { message = "Verification email sent successfully" });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationConfirmRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Token))
            return BadRequest("Email and verification token are required");

        var result = await _authService.VerifyEmailAsync(request);
        if (!result)
            return BadRequest("Invalid or expired verification token");

        return Ok(new { message = "Email verified successfully" });
    }

    [HttpPost("setup-2fa")]
    [Authorize]
    public async Task<IActionResult> SetupTwoFactor([FromBody] TwoFactorSetupRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized();

        try
        {
            var twoFactorService = HttpContext.RequestServices.GetRequiredService<ITwoFactorService>();
            var response = await twoFactorService.SetupTwoFactorAsync(userId, request.Password);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return BadRequest("Invalid password");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error setting up 2FA: {ex.Message}");
        }
    }

    [HttpPost("verify-2fa")]
    [Authorize]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorVerifyRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized();

        try
        {
            var twoFactorService = HttpContext.RequestServices.GetRequiredService<ITwoFactorService>();
            
            bool isValid = false;
            if (!string.IsNullOrEmpty(request.Code))
            {
                isValid = await twoFactorService.VerifyTwoFactorAsync(userId, request.Code);
            }
            else if (!string.IsNullOrEmpty(request.RecoveryCode))
            {
                isValid = await twoFactorService.VerifyRecoveryCodeAsync(userId, request.RecoveryCode);
            }

            if (isValid)
                return Ok(new { message = "Two-factor code verified successfully" });
            else
                return BadRequest("Invalid two-factor code");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error verifying 2FA: {ex.Message}");
        }
    }

    [HttpPost("confirm-2fa-setup")]
    [Authorize]
    public async Task<IActionResult> ConfirmTwoFactorSetup([FromBody] TwoFactorConfirmSetupRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized();

        try
        {
            var twoFactorService = HttpContext.RequestServices.GetRequiredService<ITwoFactorService>();
            var result = await twoFactorService.ConfirmSetupAsync(userId, request.Code);
            if (result)
                return Ok(new { message = "Two-factor setup confirmed and enabled" });
            else
                return BadRequest("Invalid code or setup not initiated");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error confirming 2FA setup: {ex.Message}");
        }
    }

    [HttpPost("disable-2fa")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor([FromBody] TwoFactorDisableRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized();

        try
        {
            var twoFactorService = HttpContext.RequestServices.GetRequiredService<ITwoFactorService>();
            var result = await twoFactorService.DisableTwoFactorAsync(userId, request.Password);
            
            if (result)
                return Ok(new { message = "Two-factor authentication disabled successfully" });
            else
                return BadRequest("Failed to disable two-factor authentication");
        }
        catch (UnauthorizedAccessException)
        {
            return BadRequest("Invalid password");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error disabling 2FA: {ex.Message}");
        }
    }

    [HttpPost("generate-recovery-codes")]
    [Authorize]
    public async Task<IActionResult> GenerateRecoveryCodes()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized();

        try
        {
            var twoFactorService = HttpContext.RequestServices.GetRequiredService<ITwoFactorService>();
            var response = await twoFactorService.GenerateNewRecoveryCodesAsync(userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error generating recovery codes: {ex.Message}");
        }
    }

    [HttpGet("2fa-status")]
    [Authorize]
    public async Task<IActionResult> GetTwoFactorStatus()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized();

        try
        {
            var twoFactorService = HttpContext.RequestServices.GetRequiredService<ITwoFactorService>();
            var isEnabled = await twoFactorService.IsTwoFactorEnabledAsync(userId);
            return Ok(new { isTwoFactorEnabled = isEnabled });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error checking 2FA status: {ex.Message}");
        }
    }
}

// Additional DTOs for new endpoints
public class RevokeTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class TwoFactorConfirmSetupRequest
{
    public string Code { get; set; } = string.Empty;
}
