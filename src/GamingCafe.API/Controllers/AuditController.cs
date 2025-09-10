using Microsoft.AspNetCore.Mvc;
using GamingCafe.Data;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace GamingCafe.API.Controllers
{
    [ApiController]
    [Route("api/v1/audit")]
    public class AuditController : ControllerBase
    {
        private readonly GamingCafeContext _db;

        public AuditController(GamingCafeContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Query([FromQuery] string? entityType, [FromQuery] int? entityId, [FromQuery] int? userId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (page < 1) page = 1;
            pageSize = Math.Clamp(pageSize, 1, 500);

            var q = _db.AuditLogs.AsQueryable();
            if (!string.IsNullOrWhiteSpace(entityType)) q = q.Where(a => a.EntityType == entityType);
            if (entityId.HasValue) q = q.Where(a => a.EntityId == entityId.Value);
            if (userId.HasValue) q = q.Where(a => a.UserId == userId.Value);
            if (from.HasValue) q = q.Where(a => a.Timestamp >= from.Value);
            if (to.HasValue) q = q.Where(a => a.Timestamp <= to.Value);

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(a => a.Timestamp).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(new { Total = total, Page = page, PageSize = pageSize, Items = items });
        }
    }
}
