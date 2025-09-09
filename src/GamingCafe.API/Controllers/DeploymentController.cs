using GamingCafe.API.Services;
using GamingCafe.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace GamingCafe.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Admin")]
public class DeploymentController : ControllerBase
{
    private readonly IDeploymentValidationService _deploymentValidationService;
    private readonly IBackupMonitoringService _backupMonitoringService;
    private readonly IBackupService _backupService;
    private readonly ILogger<DeploymentController> _logger;

    public DeploymentController(
        IDeploymentValidationService deploymentValidationService,
        IBackupMonitoringService backupMonitoringService,
        IBackupService backupService,
        ILogger<DeploymentController> logger)
    {
        _deploymentValidationService = deploymentValidationService;
        _backupMonitoringService = backupMonitoringService;
        _backupService = backupService;
        _logger = logger;
    }

    /// <summary>
    /// Validates the entire backup deployment configuration
    /// </summary>
    [HttpGet("validate")]
    public async Task<ActionResult<DeploymentValidationResult>> ValidateDeployment()
    {
        try
        {
            _logger.LogInformation("Starting deployment validation");
            var result = await _deploymentValidationService.ValidateBackupDeploymentAsync();
            
            if (result.IsValid)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during deployment validation");
            return StatusCode(500, new { error = "Internal server error during validation", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets comprehensive backup system health status
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<BackupHealthStatus>> GetHealthStatus()
    {
        try
        {
            var healthStatus = await _backupMonitoringService.GetBackupHealthStatusAsync();
            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health status");
            return StatusCode(500, new { error = "Internal server error retrieving health status", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets backup metrics for a specified date range
    /// </summary>
    [HttpGet("metrics")]
    public async Task<ActionResult<IEnumerable<BackupMetrics>>> GetMetrics(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
            var toDate = to ?? DateTime.UtcNow;

            var metrics = await _backupMonitoringService.GetBackupMetricsAsync(fromDate, toDate);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backup metrics");
            return StatusCode(500, new { error = "Internal server error retrieving metrics", details = ex.Message });
        }
    }

    /// <summary>
    /// Validates PostgreSQL client tools availability
    /// </summary>
    [HttpGet("validate/postgresql-tools")]
    public async Task<ActionResult<object>> ValidatePostgreSQLTools()
    {
        try
        {
            var toolsAvailable = await _deploymentValidationService.ValidatePostgreSQLToolsAsync();
            return Ok(new { PostgreSQLToolsAvailable = toolsAvailable });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PostgreSQL tools");
            return StatusCode(500, new { error = "Internal server error validating tools", details = ex.Message });
        }
    }

    /// <summary>
    /// Validates backup directory permissions
    /// </summary>
    [HttpGet("validate/permissions")]
    public async Task<ActionResult<object>> ValidatePermissions()
    {
        try
        {
            var permissionsValid = await _deploymentValidationService.ValidateBackupDirectoryPermissionsAsync();
            return Ok(new { BackupDirectoryPermissions = permissionsValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating permissions");
            return StatusCode(500, new { error = "Internal server error validating permissions", details = ex.Message });
        }
    }

    /// <summary>
    /// Validates storage capacity
    /// </summary>
    [HttpGet("validate/storage")]
    public async Task<ActionResult<object>> ValidateStorage()
    {
        try
        {
            var storageAdequate = await _deploymentValidationService.ValidateStorageCapacityAsync();
            return Ok(new { StorageCapacityAdequate = storageAdequate });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating storage");
            return StatusCode(500, new { error = "Internal server error validating storage", details = ex.Message });
        }
    }

    /// <summary>
    /// Runs a comprehensive backup/restore test
    /// </summary>
    [HttpPost("test/backup-restore")]
    public async Task<ActionResult<BackupTestResult>> TestBackupRestore()
    {
        try
        {
            _logger.LogInformation("Starting backup/restore test");
            var testResult = await _deploymentValidationService.TestBackupRestoreProcedureAsync();
            
            if (testResult.Success)
            {
                return Ok(testResult);
            }
            else
            {
                return BadRequest(testResult);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during backup/restore test");
            return StatusCode(500, new { error = "Internal server error during test", details = ex.Message });
        }
    }

    /// <summary>
    /// Validates integrity of a specific backup file
    /// </summary>
    [HttpPost("validate/backup-integrity")]
    public async Task<ActionResult<object>> ValidateBackupIntegrity([FromBody] ValidateBackupIntegrityRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.BackupFilePath))
            {
                return BadRequest(new { error = "Backup file path is required" });
            }

            var isValid = await _backupMonitoringService.ValidateBackupIntegrityAsync(request.BackupFilePath);
            return Ok(new { IsValid = isValid, BackupFilePath = request.BackupFilePath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating backup integrity");
            return StatusCode(500, new { error = "Internal server error validating backup integrity", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets deployment readiness summary
    /// </summary>
    [HttpGet("readiness")]
    public async Task<ActionResult<DeploymentReadinessReport>> GetDeploymentReadiness()
    {
        try
        {
            _logger.LogInformation("Generating deployment readiness report");

            var validation = await _deploymentValidationService.ValidateBackupDeploymentAsync();
            var health = await _backupMonitoringService.GetBackupHealthStatusAsync();
            var backups = await _backupService.GetAvailableBackupsAsync();

            var report = new DeploymentReadinessReport
            {
                ValidationResult = validation,
                HealthStatus = health,
                TotalBackups = backups.Count(),
                LastBackupDate = null, // We'll need to implement this properly
                IsProductionReady = validation.IsValid && health.OverallHealth != BackupHealth.Critical,
                RecommendedActions = GenerateRecommendedActions(validation, health)
            };

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating deployment readiness report");
            return StatusCode(500, new { error = "Internal server error generating readiness report", details = ex.Message });
        }
    }

    private List<string> GenerateRecommendedActions(DeploymentValidationResult validation, BackupHealthStatus health)
    {
        var actions = new List<string>();

        if (!validation.PostgreSQLToolsAvailable)
        {
            actions.Add("Install PostgreSQL client tools (pg_dump and psql)");
        }

        if (!validation.BackupDirectoryPermissions)
        {
            actions.Add("Fix backup directory permissions - ensure write access");
        }

        if (!validation.StorageCapacityAdequate)
        {
            actions.Add("Increase available storage space for backups");
        }

        if (!validation.BackupRestoreTest.Success)
        {
            actions.Add("Investigate and fix backup/restore test failures");
        }

        if (!validation.ConfigurationValid)
        {
            actions.Add("Review and fix backup configuration settings");
        }

        if (health.OverallHealth == BackupHealth.Warning)
        {
            actions.Add("Address backup system warnings");
        }

        if (health.OverallHealth == BackupHealth.Critical)
        {
            actions.Add("URGENT: Address critical backup system issues");
        }

        if (health.TimeSinceLastBackup.TotalHours > 24)
        {
            actions.Add("Run a manual backup to ensure system is working");
        }

        if (health.RecentFailures > 0)
        {
            actions.Add($"Investigate {health.RecentFailures} recent backup failures");
        }

        if (actions.Count == 0)
        {
            actions.Add("System is ready for production deployment");
        }

        return actions;
    }
}

public class ValidateBackupIntegrityRequest
{
    public string BackupFilePath { get; set; } = string.Empty;
}

public class DeploymentReadinessReport
{
    public DeploymentValidationResult ValidationResult { get; set; } = new();
    public BackupHealthStatus HealthStatus { get; set; } = new();
    public int TotalBackups { get; set; }
    public DateTime? LastBackupDate { get; set; }
    public bool IsProductionReady { get; set; }
    public List<string> RecommendedActions { get; set; } = new();
    public DateTime ReportDate { get; set; } = DateTime.UtcNow;
}
