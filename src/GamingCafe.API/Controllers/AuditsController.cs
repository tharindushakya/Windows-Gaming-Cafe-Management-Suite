using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GamingCafe.Core.Interfaces.Services;

namespace GamingCafe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditsController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var logs = await _auditService.GetAuditLogsAsync(page, pageSize, startDate, endDate);
        return Ok(new { page, pageSize, count = logs.Count(), items = logs });
    }

    [HttpGet("entity/{entityType}/{entityId}")]
    [Authorize(Roles = "Administrator,Admin")]
    public async Task<IActionResult> GetEntity(string entityType, int entityId)
    {
        var logs = await _auditService.GetEntityAuditLogsAsync(entityType, entityId);
        return Ok(new { entityType, entityId, items = logs });
    }
}
