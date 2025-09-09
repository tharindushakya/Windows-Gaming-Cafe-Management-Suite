using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Core.DTOs;
using System.ComponentModel.DataAnnotations;
using GamingCafe.Core.Authorization;
using Asp.Versioning;

namespace GamingCafe.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;
    private readonly ILogger<BackupController> _logger;

    public BackupController(IBackupService backupService, ILogger<BackupController> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available backups
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAvailableBackups()
    {
        try
        {
            var backups = await _backupService.GetAvailableBackupsAsync();
            return Ok(backups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available backups");
            return StatusCode(500, "An error occurred while retrieving backups");
        }
    }

    /// <summary>
    /// Create a new database backup
    /// </summary>
    [HttpPost("create")]
    public async Task<ActionResult<BackupOperationResult>> CreateBackup([FromBody] CreateBackupRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var startTime = DateTime.UtcNow;
            var success = await _backupService.CreateBackupAsync(request.Name);
            var duration = DateTime.UtcNow - startTime;

            var result = new BackupOperationResult
            {
                Success = success,
                Message = success ? "Backup created successfully" : "Backup creation failed",
                CompletedAt = success ? DateTime.UtcNow : null,
                Duration = duration
            };

            if (success)
            {
                _logger.LogInformation("Backup created successfully: {BackupName}", request.Name);
                return Ok(result);
            }
            else
            {
                _logger.LogError("Backup creation failed: {BackupName}", request.Name);
                return StatusCode(500, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup: {BackupName}", request.Name);
            return StatusCode(500, new BackupOperationResult
            {
                Success = false,
                Message = $"An error occurred while creating backup: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Restore a database from backup
    /// </summary>
    [HttpPost("restore")]
    public async Task<ActionResult<BackupOperationResult>> RestoreBackup([FromBody] RestoreBackupRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!request.ConfirmRestore)
        {
            return BadRequest(new BackupOperationResult
            {
                Success = false,
                Message = "Restore confirmation is required. This operation will overwrite existing data."
            });
        }

        try
        {
            var startTime = DateTime.UtcNow;
            var success = await _backupService.RestoreBackupAsync(request.BackupName);
            var duration = DateTime.UtcNow - startTime;

            var result = new BackupOperationResult
            {
                Success = success,
                Message = success ? "Database restored successfully" : "Database restore failed",
                CompletedAt = success ? DateTime.UtcNow : null,
                Duration = duration
            };

            if (success)
            {
                _logger.LogInformation("Database restored successfully from backup: {BackupName}", request.BackupName);
                return Ok(result);
            }
            else
            {
                _logger.LogError("Database restore failed from backup: {BackupName}", request.BackupName);
                return StatusCode(500, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring backup: {BackupName}", request.BackupName);
            return StatusCode(500, new BackupOperationResult
            {
                Success = false,
                Message = $"An error occurred while restoring backup: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Delete a backup file
    /// </summary>
    [HttpDelete("{backupName}")]
    public async Task<ActionResult<BackupOperationResult>> DeleteBackup(string backupName)
    {
        if (string.IsNullOrWhiteSpace(backupName))
            return BadRequest("Backup name is required");

        try
        {
            var success = await _backupService.DeleteBackupAsync(backupName);

            var result = new BackupOperationResult
            {
                Success = success,
                Message = success ? "Backup deleted successfully" : "Backup deletion failed"
            };

            if (success)
            {
                _logger.LogInformation("Backup deleted successfully: {BackupName}", backupName);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Backup deletion failed: {BackupName}", backupName);
                return NotFound(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup: {BackupName}", backupName);
            return StatusCode(500, new BackupOperationResult
            {
                Success = false,
                Message = $"An error occurred while deleting backup: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Schedule automatic backups
    /// </summary>
    [HttpPost("schedule")]
    public async Task<ActionResult<BackupOperationResult>> ScheduleBackup([FromBody] BackupScheduleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var success = await _backupService.ScheduleBackupAsync(request.Interval);

            var result = new BackupOperationResult
            {
                Success = success,
                Message = success 
                    ? $"Backup scheduled successfully. Interval: {request.Interval}" 
                    : "Backup scheduling failed"
            };

            if (success)
            {
                _logger.LogInformation("Backup scheduled successfully with interval: {Interval}", request.Interval);
                return Ok(result);
            }
            else
            {
                _logger.LogError("Backup scheduling failed");
                return StatusCode(500, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling backup");
            return StatusCode(500, new BackupOperationResult
            {
                Success = false,
                Message = $"An error occurred while scheduling backup: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Trigger an immediate backup (for testing/manual backup)
    /// </summary>
    [HttpPost("immediate")]
    public async Task<ActionResult<BackupOperationResult>> CreateImmediateBackup()
    {
        try
        {
            var backupName = $"manual_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var startTime = DateTime.UtcNow;
            var success = await _backupService.CreateBackupAsync(backupName);
            var duration = DateTime.UtcNow - startTime;

            var result = new BackupOperationResult
            {
                Success = success,
                Message = success 
                    ? $"Immediate backup '{backupName}' created successfully" 
                    : "Immediate backup creation failed",
                CompletedAt = success ? DateTime.UtcNow : null,
                Duration = duration
            };

            if (success)
            {
                _logger.LogInformation("Immediate backup created successfully: {BackupName}", backupName);
                return Ok(result);
            }
            else
            {
                _logger.LogError("Immediate backup creation failed: {BackupName}", backupName);
                return StatusCode(500, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating immediate backup");
            return StatusCode(500, new BackupOperationResult
            {
                Success = false,
                Message = $"An error occurred while creating immediate backup: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get backup system health status
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<object>> GetBackupHealth()
    {
        try
        {
            var backups = await _backupService.GetAvailableBackupsAsync();
            var backupList = backups.ToList();
            
            var latestBackup = backupList
                .Cast<BackupInfoDto>()
                .OrderByDescending(b => b.CreatedDate)
                .FirstOrDefault();

            var healthStatus = new
            {
                TotalBackups = backupList.Count,
                LatestBackup = latestBackup?.CreatedDate,
                LatestBackupAge = latestBackup != null 
                    ? DateTime.UtcNow - latestBackup.CreatedDate 
                    : (TimeSpan?)null,
                BackupDirectoryExists = Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Backups")),
                TotalBackupSize = backupList.Cast<BackupInfoDto>().Sum(b => b.FileSizeBytes),
                Status = latestBackup != null && (DateTime.UtcNow - latestBackup.CreatedDate).TotalDays < 7 
                    ? "Healthy" : "Warning"
            };

            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backup health status");
            return StatusCode(500, "An error occurred while checking backup health");
        }
    }
}
