namespace GamingCafe.Admin.Services;

public class AdminNotificationService
{
    private readonly List<NotificationMessage> _notifications = new();
    private event Action? OnNotificationsChanged;

    public IReadOnlyList<NotificationMessage> Notifications => _notifications.AsReadOnly();

    public void AddNotification(string message, NotificationType type = NotificationType.Info, int? durationMs = 5000)
    {
        var notification = new NotificationMessage
        {
            Id = Guid.NewGuid(),
            Message = message,
            Type = type,
            Timestamp = DateTime.Now,
            DurationMs = durationMs
        };

        _notifications.Add(notification);
        OnNotificationsChanged?.Invoke();

        // Auto-remove notification after duration
        if (durationMs.HasValue)
        {
            Task.Delay(durationMs.Value).ContinueWith(_ => RemoveNotification(notification.Id));
        }
    }

    public void AddSuccess(string message, int? durationMs = 5000)
    {
        AddNotification(message, NotificationType.Success, durationMs);
    }

    public void AddError(string message, int? durationMs = 8000)
    {
        AddNotification(message, NotificationType.Error, durationMs);
    }

    public void AddWarning(string message, int? durationMs = 6000)
    {
        AddNotification(message, NotificationType.Warning, durationMs);
    }

    public void AddInfo(string message, int? durationMs = 5000)
    {
        AddNotification(message, NotificationType.Info, durationMs);
    }

    // Convenience methods with more intuitive names
    public void ShowSuccess(string message, int? durationMs = 5000) => AddSuccess(message, durationMs);
    public void ShowError(string message, int? durationMs = 8000) => AddError(message, durationMs);
    public void ShowWarning(string message, int? durationMs = 6000) => AddWarning(message, durationMs);
    public void ShowInfo(string message, int? durationMs = 5000) => AddInfo(message, durationMs);

    public void RemoveNotification(Guid id)
    {
        var notification = _notifications.FirstOrDefault(n => n.Id == id);
        if (notification != null)
        {
            _notifications.Remove(notification);
            OnNotificationsChanged?.Invoke();
        }
    }

    public void ClearAll()
    {
        _notifications.Clear();
        OnNotificationsChanged?.Invoke();
    }

    public void Subscribe(Action callback)
    {
        OnNotificationsChanged += callback;
    }

    public void Unsubscribe(Action callback)
    {
        OnNotificationsChanged -= callback;
    }
}

public class NotificationMessage
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public int? DurationMs { get; set; }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}
