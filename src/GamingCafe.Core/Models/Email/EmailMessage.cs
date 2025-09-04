using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models.Email;

/// <summary>
/// Represents an email message with all necessary properties
/// </summary>
public class EmailMessage
{
    /// <summary>
    /// Unique identifier for tracking the email
    /// </summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Sender email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Sender display name
    /// </summary>
    public string? FromName { get; set; }

    /// <summary>
    /// Primary recipient email addresses
    /// </summary>
    [Required]
    public List<EmailAddress> To { get; set; } = new();

    /// <summary>
    /// Carbon copy recipients
    /// </summary>
    public List<EmailAddress> Cc { get; set; } = new();

    /// <summary>
    /// Blind carbon copy recipients
    /// </summary>
    public List<EmailAddress> Bcc { get; set; } = new();

    /// <summary>
    /// Reply-to email address
    /// </summary>
    public string? ReplyTo { get; set; }

    /// <summary>
    /// Email subject line
    /// </summary>
    [Required]
    [StringLength(998)] // RFC 2822 limit
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Plain text body content
    /// </summary>
    public string? TextBody { get; set; }

    /// <summary>
    /// HTML body content
    /// </summary>
    public string? HtmlBody { get; set; }

    /// <summary>
    /// Email attachments
    /// </summary>
    public List<EmailAttachment> Attachments { get; set; } = new();

    /// <summary>
    /// Custom headers
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Email priority level
    /// </summary>
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;

    /// <summary>
    /// Email category/type for tracking
    /// </summary>
    public EmailCategory Category { get; set; } = EmailCategory.Transactional;

    /// <summary>
    /// Whether to track email opens
    /// </summary>
    public bool TrackOpens { get; set; } = false;

    /// <summary>
    /// Whether to track link clicks
    /// </summary>
    public bool TrackClicks { get; set; } = false;

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Metadata for the email
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents an email address with optional display name
/// </summary>
public class EmailAddress
{
    [Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string Address { get; set; } = string.Empty;

    public string? Name { get; set; }

    public EmailAddress() { }

    public EmailAddress(string address, string? name = null)
    {
        Address = address;
        Name = name;
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(Name) ? Address : $"{Name} <{Address}>";
    }
}

/// <summary>
/// Represents an email attachment
/// </summary>
public class EmailAttachment
{
    /// <summary>
    /// File name of the attachment
    /// </summary>
    [Required]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type
    /// </summary>
    [Required]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Attachment data
    /// </summary>
    [Required]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Content ID for inline attachments
    /// </summary>
    public string? ContentId { get; set; }

    /// <summary>
    /// Whether this is an inline attachment
    /// </summary>
    public bool IsInline { get; set; } = false;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size => Data.Length;
}

/// <summary>
/// Email priority levels
/// </summary>
public enum EmailPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

/// <summary>
/// Email categories for tracking and organization
/// </summary>
public enum EmailCategory
{
    Transactional,
    Marketing,
    Notification,
    Alert,
    Welcome,
    PasswordReset,
    Promotional,
    Receipt,
    Reminder,
    System
}
