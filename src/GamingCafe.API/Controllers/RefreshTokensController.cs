using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GamingCafe.Data;

namespace GamingCafe.API.Controllers
{
    [ApiController]
    [Route("api/admin/refresh-tokens")]
    [Authorize(Roles = "Admin")]
    public class RefreshTokensController : ControllerBase
    {
        private readonly GamingCafeContext _db;

        public RefreshTokensController(GamingCafeContext db)
        {
            _db = db;
        }

        // GET: api/admin/refresh-tokens/{userId}
        [HttpGet("{userId:int}")]
        public async Task<IActionResult> ListForUser(int userId)
        {
            var tokens = await _db.RefreshTokens
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new {
                    t.TokenId,
                    t.UserId,
                    t.DeviceInfo,
                    t.IpAddress,
                    t.CreatedAt,
                    t.ExpiresAt,
                    t.RevokedAt,
                    t.ReplacedByTokenId
                })
                .ToListAsync();

            if (tokens == null || tokens.Count == 0)
                return NotFound(new { message = "No refresh tokens found for user." });

            return Ok(tokens);
        }

        // POST: api/admin/refresh-tokens/{userId}/revoke
        [HttpPost("{userId:int}/revoke")]
        public async Task<IActionResult> RevokeForUser(int userId, [FromBody] RevokeRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "Invalid request." });

            if (!string.IsNullOrEmpty(req.TokenId))
            {
                if (!Guid.TryParse(req.TokenId, out var tokenGuid))
                    return BadRequest(new { message = "Invalid TokenId" });

                var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenId == tokenGuid && t.UserId == userId);
                if (token == null)
                    return NotFound(new { message = "Token not found" });

                token.RevokedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return Ok(new { message = "Token revoked" });
            }

            if (req.RevokeAll)
            {
                var tokens = await _db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync();
                foreach (var t in tokens)
                    t.RevokedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return Ok(new { message = $"Revoked {tokens.Count} tokens" });
            }

            return BadRequest(new { message = "Specify TokenId or set RevokeAll = true" });
        }

        public class RevokeRequest
        {
            public string? TokenId { get; set; }
            public bool RevokeAll { get; set; }
        }
    }
}
