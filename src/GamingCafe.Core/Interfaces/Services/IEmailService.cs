using GamingCafe.Core.Models.Email;

namespace GamingCafe.Core.Interfaces.Services;

/// <summary>
/// Comprehensive email service interface for the Gaming Cafe application
/// Supports transactional emails, notifications, and bulk communications
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a single email message
    /// </summary>
    /// <param name="emailMessage">Email message to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<EmailResult> SendEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send bulk emails (with rate limiting and batch processing)
    /// </summary>
    /// <param name="emailMessages">Collection of email messages to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk email result with success/failure details</returns>
    Task<BulkEmailResult> SendBulkEmailAsync(IEnumerable<EmailMessage> emailMessages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send templated email using predefined templates
    /// </summary>
    /// <param name="templateRequest">Template email request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<EmailResult> SendTemplatedEmailAsync(TemplatedEmailRequest templateRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate email address format and deliverability
    /// </summary>
    /// <param name="emailAddress">Email address to validate</param>
    /// <param name="checkDeliverability">Whether to check if email is deliverable</param>
    /// <returns>Validation result</returns>
    Task<EmailValidationResult> ValidateEmailAsync(string emailAddress, bool checkDeliverability = false);

    /// <summary>
    /// Test SMTP connection and configuration
    /// </summary>
    /// <returns>Health check result for SMTP service</returns>
    Task<EmailHealthResult> TestConnectionAsync();

    /// <summary>
    /// Get email delivery statistics
    /// </summary>
    /// <param name="fromDate">Start date for statistics</param>
    /// <param name="toDate">End date for statistics</param>
    /// <returns>Email delivery statistics</returns>
    Task<EmailStatistics> GetEmailStatisticsAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Queue email for later delivery (useful for high-volume scenarios)
    /// </summary>
    /// <param name="emailMessage">Email message to queue</param>
    /// <param name="scheduledTime">When to send the email (null for immediate)</param>
    /// <returns>Queue result with tracking ID</returns>
    Task<EmailQueueResult> QueueEmailAsync(EmailMessage emailMessage, DateTime? scheduledTime = null);
}
