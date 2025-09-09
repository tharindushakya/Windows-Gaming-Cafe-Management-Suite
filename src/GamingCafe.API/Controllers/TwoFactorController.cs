using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Core.DTOs;

namespace GamingCafe.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TwoFactorController : ControllerBase
{
    private readonly ITwoFactorService _twoFactorService;

    public TwoFactorController(ITwoFactorService twoFactorService)
    {
        _twoFactorService = twoFactorService;
    }

    private int? GetUserIdFromClaims()
    {
        var claim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(claim, out var id)) return id;
        return null;
    }

    [HttpPost("setup")]
    [Authorize]
    public async Task<IActionResult> Setup([FromBody] TwoFactorSetupRequest request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var result = await _twoFactorService.SetupTwoFactorAsync(userId.Value, request.Password);
        return Ok(result);
    }

    [HttpPost("verify")]
    [Authorize]
    public async Task<IActionResult> Verify([FromBody] TwoFactorVerifyRequest request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        if (!string.IsNullOrEmpty(request.RecoveryCode))
        {
            var ok = await _twoFactorService.VerifyRecoveryCodeAsync(userId.Value, request.RecoveryCode);
            return Ok(new { success = ok });
        }

        var res = await _twoFactorService.VerifyTwoFactorAsync(userId.Value, request.Code);
        return Ok(new { success = res });
    }

    [HttpPost("disable")]
    [Authorize]
    public async Task<IActionResult> Disable([FromBody] TwoFactorDisableRequest request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var ok = await _twoFactorService.DisableTwoFactorAsync(userId.Value, request.Password);
        return Ok(new { success = ok });
    }

    [HttpPost("regen-codes")]
    [Authorize]
    public async Task<IActionResult> RegenerateRecoveryCodes()
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var res = await _twoFactorService.GenerateNewRecoveryCodesAsync(userId.Value);
        return Ok(res);
    }

    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> Status()
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var enabled = await _twoFactorService.IsTwoFactorEnabledAsync(userId.Value);
        return Ok(new { isTwoFactorEnabled = enabled });
    }
}
