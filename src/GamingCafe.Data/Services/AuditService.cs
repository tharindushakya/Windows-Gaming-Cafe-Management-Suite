using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Core.Models;

namespace GamingCafe.Data.Services;

/// <summary>
/// Comprehensive audit service for tracking user actions and system changes
/// </summary>
public class AuditService : IAuditService
{
    private readonly GamingCafeContext _context;
    private readonly ILogger<AuditService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuditService(GamingCafeContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };
    }

    public async Task LogActionAsync(string action, int? userId, string? details = null)
    {
        try
        {
            var auditLog = new GamingCafe.Core.Models.AuditLog
            {
                Action = action,
                UserId = userId,
                Details = details,
                Timestamp = DateTime.UtcNow,
                IpAddress = GetCurrentIpAddress(),
                UserAgent = GetCurrentUserAgent()
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Action logged: {Action} by User {UserId}", action, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log action: {Action} for User {UserId}", action, userId);
        }
    }

    public async Task LogEntityChangeAsync(string entityType, int entityId, string action, object? oldValues = null, object? newValues = null)
    {
        try
        {
            var changeDetails = new
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                Timestamp = DateTime.UtcNow
            };

            var detailsJson = JsonSerializer.Serialize(changeDetails, _jsonOptions);

            var auditLog = new GamingCafe.Core.Models.AuditLog
            {
                Action = $"{action}_{entityType}",
                EntityType = entityType,
                EntityId = entityId,
                Details = detailsJson,
                Timestamp = DateTime.UtcNow,
                IpAddress = GetCurrentIpAddress(),
                UserAgent = GetCurrentUserAgent()
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Entity change logged: {Action} on {EntityType} {EntityId}", action, entityType, entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log entity change: {Action} on {EntityType} {EntityId}", action, entityType, entityId);
        }
    }

    public async Task<IEnumerable<object>> GetAuditLogsAsync(int page, int pageSize, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.AuditLogs.AsQueryable();

            // Apply date filters
            if (startDate.HasValue)
                query = query.Where(log => log.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(log => log.Timestamp <= endDate.Value);

            var logs = await query
                .OrderByDescending(log => log.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(log => new
                {
                    log.AuditLogId,
                    log.Action,
                    log.UserId,
                    UserName = log.User != null ? log.User.Username : "System",
                    log.EntityType,
                    log.EntityId,
                    log.Details,
                    log.Timestamp,
                    log.IpAddress,
                    log.UserAgent
                })
                .ToListAsync();

            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit logs");
            return new List<object>();
        }
    }

    public async Task<IEnumerable<object>> GetEntityAuditLogsAsync(string entityType, int entityId)
    {
        try
        {
            var logs = await _context.AuditLogs
                .Where(log => log.EntityType == entityType && log.EntityId == entityId)
                .OrderByDescending(log => log.Timestamp)
                .Select(log => new
                {
                    log.AuditLogId,
                    log.Action,
                    log.UserId,
                    UserName = log.User != null ? log.User.Username : "System",
                    log.Details,
                    log.Timestamp,
                    log.IpAddress
                })
                .ToListAsync();

            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve entity audit logs for {EntityType} {EntityId}", entityType, entityId);
            return new List<object>();
        }
    }

    private string? GetCurrentIpAddress()
    {
        // Placeholder implementation; IHttpContextAccessor not available in Data project
        return "127.0.0.1"; 
    }
    
    private string? GetCurrentUserAgent()
    {
        // Placeholder implementation; IHttpContextAccessor not available in Data project
        return "System";
    }
}

/// <summary>
/// Audit log entity for tracking user actions and system changes
/// </summary>
public class AuditLog
{
    public int AuditLogId { get; set; }
    public string Action { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
