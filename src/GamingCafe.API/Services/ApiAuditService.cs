using GamingCafe.Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace GamingCafe.API.Services;

/// <summary>
/// API-level audit service that enriches audit entries with request context
/// and delegates to the Data project's AuditService implementation.
/// Keeps the Data layer free of ASP.NET dependencies.
/// </summary>
public class ApiAuditService : IAuditService
{
    private readonly IAuditService _inner; // Data layer implementation
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiAuditService(IAuditService inner, IHttpContextAccessor httpContextAccessor)
    {
        _inner = inner;
        _httpContextAccessor = httpContextAccessor;
    }

    private (int? userId, string details) Enrich(string? details)
    {
        var ctx = _httpContextAccessor.HttpContext;
        int? userId = null;
        try
        {
            var claim = ctx?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out var id)) userId = id;
        }
        catch { }

        var ip = ctx?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = ctx?.Request?.Headers?["User-Agent"].ToString() ?? "unknown";

    var enriched = $"{details ?? string.Empty} | ip={ip} | ua={ua}";
        return (userId, enriched);
    }

    public async Task LogActionAsync(string action, int? userId, string? details = null)
    {
        var (uid, enriched) = Enrich(details);
        if (_inner != null)
            await _inner.LogActionAsync(action, userId ?? uid, enriched);
    }

    public async Task LogEntityChangeAsync(string entityType, int entityId, string action, object? oldValues = null, object? newValues = null)
    {
        var (uid, enriched) = Enrich(null);
        var payload = new { Old = oldValues, New = newValues };
        var details = System.Text.Json.JsonSerializer.Serialize(payload);
        await _inner.LogEntityChangeAsync(entityType, entityId, action + " | " + enriched, null, payload);
    }

    public async Task<IEnumerable<object>> GetAuditLogsAsync(int page, int pageSize, DateTime? startDate = null, DateTime? endDate = null)
    {
        return await _inner.GetAuditLogsAsync(page, pageSize, startDate, endDate);
    }

    public async Task<IEnumerable<object>> GetEntityAuditLogsAsync(string entityType, int entityId)
    {
        return await _inner.GetEntityAuditLogsAsync(entityType, entityId);
    }
}
