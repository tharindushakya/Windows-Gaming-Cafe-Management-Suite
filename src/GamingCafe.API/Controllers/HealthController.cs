using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;

namespace GamingCafe.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(HealthCheckService healthCheckService, ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the overall health status of the application
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<HealthReportResponse>> GetHealth()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            var response = MapHealthReport(healthReport);

            var statusCode = healthReport.Status switch
            {
                HealthStatus.Healthy => HttpStatusCode.OK,
                HealthStatus.Degraded => HttpStatusCode.OK, // Still OK but with warnings
                HealthStatus.Unhealthy => HttpStatusCode.ServiceUnavailable,
                _ => HttpStatusCode.InternalServerError
            };

            return StatusCode((int)statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking application health");
            return StatusCode(500, new HealthReportResponse
            {
                Status = "Unhealthy",
                TotalDuration = "0:00:00",
                Entries = new Dictionary<string, HealthEntryResponse>
                {
                    ["Error"] = new HealthEntryResponse
                    {
                        Status = "Unhealthy",
                        Description = $"Health check failed: {ex.Message}",
                        Duration = "0:00:00"
                    }
                }
            });
        }
    }

    /// <summary>
    /// Gets detailed health information for all components
    /// </summary>
    [HttpGet("detailed")]
    public async Task<ActionResult<DetailedHealthResponse>> GetDetailedHealth()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            var response = new DetailedHealthResponse
            {
                Status = healthReport.Status.ToString(),
                TotalDuration = healthReport.TotalDuration.ToString(@"mm\:ss\.fff"),
                CheckedAt = DateTime.UtcNow,
                Components = healthReport.Entries.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new ComponentHealth
                    {
                        Status = kvp.Value.Status.ToString(),
                        Description = kvp.Value.Description ?? "No description",
                        Duration = kvp.Value.Duration.ToString(@"mm\:ss\.fff"),
                        Data = kvp.Value.Data,
                        Tags = kvp.Value.Tags?.ToArray() ?? Array.Empty<string>(),
                        Exception = kvp.Value.Exception?.Message
                    }
                ),
                Summary = new HealthSummary
                {
                    TotalChecks = healthReport.Entries.Count,
                    HealthyChecks = healthReport.Entries.Count(e => e.Value.Status == HealthStatus.Healthy),
                    DegradedChecks = healthReport.Entries.Count(e => e.Value.Status == HealthStatus.Degraded),
                    UnhealthyChecks = healthReport.Entries.Count(e => e.Value.Status == HealthStatus.Unhealthy)
                }
            };

            var statusCode = healthReport.Status switch
            {
                HealthStatus.Healthy => HttpStatusCode.OK,
                HealthStatus.Degraded => HttpStatusCode.OK,
                HealthStatus.Unhealthy => HttpStatusCode.ServiceUnavailable,
                _ => HttpStatusCode.InternalServerError
            };

            return StatusCode((int)statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting detailed health information");
            return StatusCode(500, new { error = "Failed to get detailed health information", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets health status for a specific component
    /// </summary>
    [HttpGet("component/{componentName}")]
    public async Task<ActionResult<ComponentHealth>> GetComponentHealth(string componentName)
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            if (!healthReport.Entries.TryGetValue(componentName, out var entry))
            {
                return NotFound(new { error = $"Component '{componentName}' not found" });
            }

            var response = new ComponentHealth
            {
                Status = entry.Status.ToString(),
                Description = entry.Description ?? "No description",
                Duration = entry.Duration.ToString(@"mm\:ss\.fff"),
                Data = entry.Data,
                Tags = entry.Tags?.ToArray() ?? Array.Empty<string>(),
                Exception = entry.Exception?.Message
            };

            var statusCode = entry.Status switch
            {
                HealthStatus.Healthy => HttpStatusCode.OK,
                HealthStatus.Degraded => HttpStatusCode.OK,
                HealthStatus.Unhealthy => HttpStatusCode.ServiceUnavailable,
                _ => HttpStatusCode.InternalServerError
            };

            return StatusCode((int)statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking component health for {ComponentName}", componentName);
            return StatusCode(500, new { error = "Failed to check component health", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets a simple ready/not ready status for load balancers
    /// </summary>
    [HttpGet("ready")]
    public async Task<ActionResult> GetReadiness()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            if (healthReport.Status == HealthStatus.Healthy)
            {
                return Ok(new { status = "Ready", timestamp = DateTime.UtcNow });
            }
            else
            {
                return StatusCode(503, new { 
                    status = "Not Ready", 
                    reason = healthReport.Status.ToString(),
                    timestamp = DateTime.UtcNow 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking readiness");
            return StatusCode(503, new { 
                status = "Not Ready", 
                reason = "Health check failed",
                error = ex.Message,
                timestamp = DateTime.UtcNow 
            });
        }
    }

    /// <summary>
    /// Gets a simple alive status for monitoring
    /// </summary>
    [HttpGet("alive")]
    public ActionResult GetLiveness()
    {
        return Ok(new { 
            status = "Alive", 
            timestamp = DateTime.UtcNow,
            uptime = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime
        });
    }

    private static HealthReportResponse MapHealthReport(HealthReport healthReport)
    {
        return new HealthReportResponse
        {
            Status = healthReport.Status.ToString(),
            TotalDuration = healthReport.TotalDuration.ToString(@"mm\:ss\.fff"),
            Entries = healthReport.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new HealthEntryResponse
                {
                    Status = kvp.Value.Status.ToString(),
                    Description = kvp.Value.Description ?? "No description",
                    Duration = kvp.Value.Duration.ToString(@"mm\:ss\.fff"),
                    Data = kvp.Value.Data,
                    Exception = kvp.Value.Exception?.Message
                }
            )
        };
    }
}

// Response DTOs
public class HealthReportResponse
{
    public string Status { get; set; } = string.Empty;
    public string TotalDuration { get; set; } = string.Empty;
    public Dictionary<string, HealthEntryResponse> Entries { get; set; } = new();
}

public class HealthEntryResponse
{
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, object>? Data { get; set; }
    public string? Exception { get; set; }
}

public class DetailedHealthResponse
{
    public string Status { get; set; } = string.Empty;
    public string TotalDuration { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();
    public HealthSummary Summary { get; set; } = new();
}

public class ComponentHealth
{
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, object>? Data { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string? Exception { get; set; }
}

public class HealthSummary
{
    public int TotalChecks { get; set; }
    public int HealthyChecks { get; set; }
    public int DegradedChecks { get; set; }
    public int UnhealthyChecks { get; set; }
    public double HealthPercentage => TotalChecks > 0 ? (double)HealthyChecks / TotalChecks * 100 : 0;
}
