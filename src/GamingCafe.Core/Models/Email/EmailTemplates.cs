using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.Models.Email;

/// <summary>
/// Request for sending templated emails
/// </summary>
public class TemplatedEmailRequest
{
    /// <summary>
    /// Template identifier
    /// </summary>
    [Required]
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Recipient email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Recipient display name
    /// </summary>
    public string? ToName { get; set; }

    /// <summary>
    /// Template variables/placeholders
    /// </summary>
    public Dictionary<string, object> Variables { get; set; } = new();

    /// <summary>
    /// Email category for tracking
    /// </summary>
    public EmailCategory Category { get; set; } = EmailCategory.Transactional;

    /// <summary>
    /// Whether to track opens and clicks
    /// </summary>
    public bool EnableTracking { get; set; } = false;

    /// <summary>
    /// Custom metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Override sender information
    /// </summary>
    public string? FromOverride { get; set; }

    /// <summary>
    /// Override sender name
    /// </summary>
    public string? FromNameOverride { get; set; }

    /// <summary>
    /// Language/locale for the template
    /// </summary>
    public string Language { get; set; } = "en";
}

/// <summary>
/// Email template definition
/// </summary>
public class EmailTemplate
{
    /// <summary>
    /// Unique template identifier
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Template name
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Subject line template
    /// </summary>
    [Required]
    public string SubjectTemplate { get; set; } = string.Empty;

    /// <summary>
    /// HTML body template
    /// </summary>
    public string? HtmlTemplate { get; set; }

    /// <summary>
    /// Plain text body template
    /// </summary>
    public string? TextTemplate { get; set; }

    /// <summary>
    /// Template category
    /// </summary>
    public EmailCategory Category { get; set; }

    /// <summary>
    /// Supported languages
    /// </summary>
    public List<string> SupportedLanguages { get; set; } = new() { "en" };

    /// <summary>
    /// Template variables with descriptions
    /// </summary>
    public List<TemplateVariable> Variables { get; set; } = new();

    /// <summary>
    /// Whether the template is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Template version
    /// </summary>
    public int Version { get; set; } = 1;
}

/// <summary>
/// Template variable definition
/// </summary>
public class TemplateVariable
{
    /// <summary>
    /// Variable name (placeholder)
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Variable description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Variable data type
    /// </summary>
    public TemplateVariableType Type { get; set; } = TemplateVariableType.String;

    /// <summary>
    /// Whether the variable is required
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Default value
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Validation rules
    /// </summary>
    public List<string> ValidationRules { get; set; } = new();
}

/// <summary>
/// Template variable types
/// </summary>
public enum TemplateVariableType
{
    String,
    Number,
    Boolean,
    Date,
    Url,
    Email,
    Currency,
    Array,
    Object
}

/// <summary>
/// SMTP configuration settings
/// </summary>
public class SmtpConfiguration
{
    /// <summary>
    /// SMTP server host
    /// </summary>
    [Required]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    /// <summary>
    /// Whether to use SSL/TLS
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Authentication username
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Authentication password
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Default sender email
    /// </summary>
    [Required]
    [EmailAddress]
    public string DefaultFromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Default sender name
    /// </summary>
    public string? DefaultFromName { get; set; }

    /// <summary>
    /// Connection timeout in milliseconds
    /// </summary>
    [Range(1000, 300000)]
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Maximum number of simultaneous connections
    /// </summary>
    [Range(1, 100)]
    public int MaxConnections { get; set; } = 5;

    /// <summary>
    /// Rate limiting - max emails per minute
    /// </summary>
    [Range(1, 10000)]
    public int MaxEmailsPerMinute { get; set; } = 100;

    /// <summary>
    /// Whether to enable delivery status notifications
    /// </summary>
    public bool EnableDeliveryNotifications { get; set; } = false;

    /// <summary>
    /// Bounce handling email address
    /// </summary>
    [EmailAddress]
    public string? BounceEmail { get; set; }
}
