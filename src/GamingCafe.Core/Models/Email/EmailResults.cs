namespace GamingCafe.Core.Models.Email;

/// <summary>
/// Result of an email sending operation
/// </summary>
public class EmailResult
{
    /// <summary>
    /// Whether the email was sent successfully
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Message ID assigned by the email service
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Error message if sending failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Detailed error information
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Timestamp when the email was sent
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Create a successful result
    /// </summary>
    public static EmailResult Success(string messageId)
    {
        return new EmailResult
        {
            IsSuccess = true,
            MessageId = messageId,
            SentAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Create a failed result
    /// </summary>
    public static EmailResult Failure(string errorMessage, Exception? exception = null)
    {
        return new EmailResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception
        };
    }
}

/// <summary>
/// Result of a bulk email operation
/// </summary>
public class BulkEmailResult
{
    /// <summary>
    /// Total number of emails processed
    /// </summary>
    public int TotalEmails { get; set; }

    /// <summary>
    /// Number of emails sent successfully
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of emails that failed to send
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Individual email results
    /// </summary>
    public List<EmailResult> Results { get; set; } = new();

    /// <summary>
    /// Overall operation success rate
    /// </summary>
    public double SuccessRate => TotalEmails > 0 ? (double)SuccessCount / TotalEmails : 0;

    /// <summary>
    /// Time taken to process all emails
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Whether the bulk operation was completely successful
    /// </summary>
    public bool IsCompleteSuccess => FailureCount == 0;
}

/// <summary>
/// Email validation result
/// </summary>
public class EmailValidationResult
{
    /// <summary>
    /// Whether the email format is valid
    /// </summary>
    public bool IsValidFormat { get; set; }

    /// <summary>
    /// Whether the email domain exists
    /// </summary>
    public bool DomainExists { get; set; }

    /// <summary>
    /// Whether the email is deliverable (if checked)
    /// </summary>
    public bool? IsDeliverable { get; set; }

    /// <summary>
    /// Validation error messages
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Overall validity
    /// </summary>
    public bool IsValid => IsValidFormat && DomainExists && (IsDeliverable ?? true);
}

/// <summary>
/// SMTP health check result
/// </summary>
public class EmailHealthResult
{
    /// <summary>
    /// Whether SMTP connection is healthy
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// SMTP server response time
    /// </summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>
    /// Error message if connection failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// SMTP server information
    /// </summary>
    public SmtpServerInfo? ServerInfo { get; set; }

    /// <summary>
    /// Last check timestamp
    /// </summary>
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// SMTP server information
/// </summary>
public class SmtpServerInfo
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool UseSsl { get; set; }
    public string? ServerGreeting { get; set; }
    public List<string> SupportedExtensions { get; set; } = new();
}

/// <summary>
/// Email delivery statistics
/// </summary>
public class EmailStatistics
{
    /// <summary>
    /// Date range for statistics
    /// </summary>
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Total emails sent
    /// </summary>
    public int TotalSent { get; set; }

    /// <summary>
    /// Total emails delivered successfully
    /// </summary>
    public int TotalDelivered { get; set; }

    /// <summary>
    /// Total emails that bounced
    /// </summary>
    public int TotalBounced { get; set; }

    /// <summary>
    /// Total emails opened (if tracking enabled)
    /// </summary>
    public int TotalOpened { get; set; }

    /// <summary>
    /// Total link clicks (if tracking enabled)
    /// </summary>
    public int TotalClicks { get; set; }

    /// <summary>
    /// Delivery rate percentage
    /// </summary>
    public double DeliveryRate => TotalSent > 0 ? (double)TotalDelivered / TotalSent * 100 : 0;

    /// <summary>
    /// Open rate percentage
    /// </summary>
    public double OpenRate => TotalDelivered > 0 ? (double)TotalOpened / TotalDelivered * 100 : 0;

    /// <summary>
    /// Click-through rate percentage
    /// </summary>
    public double ClickRate => TotalOpened > 0 ? (double)TotalClicks / TotalOpened * 100 : 0;

    /// <summary>
    /// Statistics by email category
    /// </summary>
    public Dictionary<EmailCategory, CategoryStatistics> CategoryStats { get; set; } = new();
}

/// <summary>
/// Statistics for a specific email category
/// </summary>
public class CategoryStatistics
{
    public int Sent { get; set; }
    public int Delivered { get; set; }
    public int Bounced { get; set; }
    public int Opened { get; set; }
    public int Clicked { get; set; }
}

/// <summary>
/// Email queue result
/// </summary>
public class EmailQueueResult
{
    /// <summary>
    /// Queue tracking ID
    /// </summary>
    public string QueueId { get; set; } = string.Empty;

    /// <summary>
    /// Status of the queued email
    /// </summary>
    public EmailQueueStatus Status { get; set; }

    /// <summary>
    /// When the email was queued
    /// </summary>
    public DateTime QueuedAt { get; set; }

    /// <summary>
    /// Scheduled send time
    /// </summary>
    public DateTime? ScheduledFor { get; set; }

    /// <summary>
    /// When the email was delivered
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Next retry time if applicable
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Queue position (if applicable)
    /// </summary>
    public int? QueuePosition { get; set; }

    /// <summary>
    /// Estimated delivery time
    /// </summary>
    public DateTime? EstimatedDeliveryTime { get; set; }
}

/// <summary>
/// Status of queued email
/// </summary>
public enum EmailQueueStatus
{
    Queued,
    Processing,
    Sent,
    Failed,
    Cancelled,
    Retrying
}
