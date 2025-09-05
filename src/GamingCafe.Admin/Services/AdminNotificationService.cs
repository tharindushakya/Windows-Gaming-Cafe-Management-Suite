using Microsoft.AspNetCore.SignalR;

namespace GamingCafe.Admin.Services;

public class AdminNotificationService
{
    private readonly ILogger<AdminNotificationService> _logger;
    private readonly List<AdminNotification> _notifications = new();
    private static int _nextNotificationId = 1000;

    public AdminNotificationService(ILogger<AdminNotificationService> logger)
    {
        _logger = logger;
    }

    public event Action<int>? OnNotificationAdded;

    public Task AddNotificationAsync(string title, string message, NotificationType type = NotificationType.Info)
    {
        var timestamp = DateTime.UtcNow;
        var notification = new AdminNotification
        {
            Id = System.Threading.Interlocked.Increment(ref _nextNotificationId),
            Title = title,
            Message = message,
            Type = type,
            Timestamp = timestamp,
            IsRead = false
        };

        _notifications.Add(notification);
        _logger.LogInformation("Admin notification added (Id:{Id}) {Title} - {Message}", notification.Id, title, message);

        try
        {
            OnNotificationAdded?.Invoke(notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while invoking OnNotificationAdded event");
        }

        return Task.CompletedTask;
    }

    public Task<int> GetUnreadCountAsync()
    {
    var count = _notifications.Count(n => !n.IsRead);
    _logger.LogDebug("AdminNotificationService.GetUnreadCountAsync -> {Count}", count);
    return Task.FromResult(count);
    }

    public IReadOnlyList<AdminNotification> GetRecentNotifications(int count = 10)
    {
        return _notifications.OrderByDescending(n => n.Timestamp).Take(count).ToList();
    }

    public Task MarkAsReadAsync(int notificationId)
    {
        var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            _logger.LogInformation("Notification {Id} marked as read", notificationId);
        }

        return Task.CompletedTask;
    }

    public void ClearNotifications()
    {
        _notifications.Clear();
    }
}

public class AdminNotification
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

public class AdminHub : Hub
{
    private readonly ILogger<AdminHub> _logger;

    public AdminHub(ILogger<AdminHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        _logger.LogInformation("Admin {ConnectionId} joined admin group", Context.ConnectionId);
    }

    public async Task LeaveAdminGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
        _logger.LogInformation("Admin {ConnectionId} left admin group", Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
        await base.OnDisconnectedAsync(exception);
    }
}
