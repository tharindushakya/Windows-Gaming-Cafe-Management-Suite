using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using GamingCafe.Core.Interfaces.Services;

namespace GamingCafe.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TestBackupController : ControllerBase
{
    private readonly IBackupService _backupService;
    private readonly ILogger<TestBackupController> _logger;

    public TestBackupController(IBackupService backupService, ILogger<TestBackupController> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint to check backup service health (no auth required for testing)
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<object>> TestBackupHealth()
    {
        try
        {
            var backups = await _backupService.GetAvailableBackupsAsync();
            var backupList = backups.ToList();
            
            var healthStatus = new
            {
                ServiceWorking = true,
                TotalBackups = backupList.Count,
                BackupDirectoryExists = Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Backups")),
                Message = "Backup service is operational"
            };

            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing backup service");
            return Ok(new
            {
                ServiceWorking = false,
                Error = ex.Message,
                Message = "Backup service encountered an error"
            });
        }
    }

    /// <summary>
    /// Test endpoint to create a simple backup (no auth required for testing)
    /// </summary>
    [HttpPost("test-create")]
    public async Task<ActionResult<object>> TestCreateBackup()
    {
        try
        {
            var backupName = $"test_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var startTime = DateTime.UtcNow;
            
            _logger.LogInformation("Testing backup creation: {BackupName}", backupName);
            
            var success = await _backupService.CreateBackupAsync(backupName);
            var duration = DateTime.UtcNow - startTime;

            var result = new
            {
                Success = success,
                BackupName = backupName,
                Duration = duration.TotalSeconds,
                Message = success ? "Test backup created successfully" : "Test backup creation failed"
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test backup");
            return Ok(new
            {
                Success = false,
                Error = ex.Message,
                Message = "Test backup creation failed"
            });
        }
    }
}
