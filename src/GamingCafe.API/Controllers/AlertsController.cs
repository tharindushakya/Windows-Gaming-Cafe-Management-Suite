using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace GamingCafe.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AlertsController : ControllerBase
{
    // Lightweight placeholder endpoint to avoid 404s from frontend apps.
    [HttpGet]
    public IActionResult GetAlerts()
    {
        var alerts = new List<object>(); // shape can be extended later
        return Ok(alerts);
    }
}
