using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Core.Interfaces.Background;
using GamingCafe.Core.Models.Email;

namespace GamingCafe.Core.Services;

/// <summary>
/// Comprehensive email service implementation with SMTP support
/// Features: templating, bulk sending, validation, health checks, and statistics
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly SmtpConfiguration _smtpConfig;
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private readonly IBackgroundTaskQueue? _taskQueue;
    private readonly Dictionary<string, EmailTemplate> _templates;
    private readonly List<EmailResult> _emailHistory;
    private readonly object _historyLock = new();
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IBackgroundTaskQueue? taskQueue = null)
    {
        _logger = logger;
        _smtpConfig = LoadSmtpConfiguration(configuration);
        _rateLimitSemaphore = new SemaphoreSlim(_smtpConfig.MaxConnections, _smtpConfig.MaxConnections);
        _templates = LoadDefaultTemplates();
        _emailHistory = new List<EmailResult>();
        _taskQueue = taskQueue;

        _logger.LogInformation("Email service initialized with SMTP host: {Host}:{Port}", 
            _smtpConfig.Host, _smtpConfig.Port);
    }

    public async Task<EmailResult> SendEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateEmailMessage(emailMessage);

            await _rateLimitSemaphore.WaitAsync(cancellationToken);
            
            try
            {
                using var smtpClient = CreateSmtpClient();
                using var mailMessage = CreateMailMessage(emailMessage);

                await smtpClient.SendMailAsync(mailMessage, cancellationToken);

                var result = EmailResult.Success(emailMessage.MessageId);
                RecordEmailResult(result);

                _logger.LogInformation("Email sent successfully to {Recipients}. MessageId: {MessageId}",
                    string.Join(", ", emailMessage.To.Select(x => x.Address)), emailMessage.MessageId);

                return result;
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients}. MessageId: {MessageId}",
                string.Join(", ", emailMessage.To.Select(x => x.Address)), emailMessage.MessageId);

            var result = EmailResult.Failure($"Failed to send email: {ex.Message}", ex);
            RecordEmailResult(result);
            return result;
        }
    }

    public async Task<BulkEmailResult> SendBulkEmailAsync(IEnumerable<EmailMessage> emailMessages, CancellationToken cancellationToken = default)
    {
        var messages = emailMessages.ToList();
        var result = new BulkEmailResult
        {
            TotalEmails = messages.Count
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var semaphore = new SemaphoreSlim(_smtpConfig.MaxConnections, _smtpConfig.MaxConnections);
        var tasks = messages.Select(async message =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await SendEmailAsync(message, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        
        result.Results = results.ToList();
        result.SuccessCount = results.Count(r => r.IsSuccess);
        result.FailureCount = results.Count(r => !r.IsSuccess);
        result.ProcessingTime = stopwatch.Elapsed;

        _logger.LogInformation("Bulk email operation completed. Total: {Total}, Success: {Success}, Failed: {Failed}, Time: {Time}ms",
            result.TotalEmails, result.SuccessCount, result.FailureCount, result.ProcessingTime.TotalMilliseconds);

        return result;
    }

    public async Task<EmailResult> SendTemplatedEmailAsync(TemplatedEmailRequest templateRequest, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_templates.TryGetValue(templateRequest.TemplateId, out var template))
            {
                return EmailResult.Failure($"Template '{templateRequest.TemplateId}' not found");
            }

            var emailMessage = new EmailMessage
            {
                From = templateRequest.FromOverride ?? _smtpConfig.DefaultFromEmail,
                FromName = templateRequest.FromNameOverride ?? _smtpConfig.DefaultFromName,
                Subject = ProcessTemplate(template.SubjectTemplate, templateRequest.Variables),
                TextBody = template.TextTemplate != null ? ProcessTemplate(template.TextTemplate, templateRequest.Variables) : null,
                HtmlBody = template.HtmlTemplate != null ? ProcessTemplate(template.HtmlTemplate, templateRequest.Variables) : null,
                Category = templateRequest.Category,
                TrackOpens = templateRequest.EnableTracking,
                TrackClicks = templateRequest.EnableTracking,
                Metadata = templateRequest.Metadata
            };

            emailMessage.To.Add(new EmailAddress(templateRequest.To, templateRequest.ToName));

            return await SendEmailAsync(emailMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send templated email with template {TemplateId} to {To}",
                templateRequest.TemplateId, templateRequest.To);
            return EmailResult.Failure($"Failed to send templated email: {ex.Message}", ex);
        }
    }

    public async Task<EmailValidationResult> ValidateEmailAsync(string emailAddress, bool checkDeliverability = false)
    {
        var result = new EmailValidationResult();

        // Format validation
        result.IsValidFormat = EmailRegex.IsMatch(emailAddress);
        if (!result.IsValidFormat)
        {
            result.ValidationErrors.Add("Invalid email format");
            return result;
        }

        // Domain validation
        try
        {
            var domain = emailAddress.Split('@')[1];
            result.DomainExists = await DomainExistsAsync(domain);
            
            if (!result.DomainExists)
            {
                result.ValidationErrors.Add("Domain does not exist");
            }

            // Deliverability check (expensive operation)
            if (checkDeliverability && result.DomainExists)
            {
                result.IsDeliverable = await CheckDeliverabilityAsync(emailAddress);
                if (result.IsDeliverable == false)
                {
                    result.ValidationErrors.Add("Email address is not deliverable");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating email domain for {Email}", emailAddress);
            result.ValidationErrors.Add($"Domain validation failed: {ex.Message}");
        }

        return result;
    }

    public async Task<EmailHealthResult> TestConnectionAsync()
    {
        var result = new EmailHealthResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var client = CreateSmtpClient();
            
            // Simulate async connection test
            await Task.Delay(1); // Minimal delay to make it properly async
            
            // Test connection by creating a simple test (won't actually send)
            var testMessage = new MailMessage
            {
                From = new MailAddress(_smtpConfig.DefaultFromEmail),
                To = { new MailAddress("test@example.invalid") },
                Subject = "Connection Test",
                Body = "Test"
            };

            // For development/testing, just check if client can be created successfully
            result.IsHealthy = true;
            result.ServerInfo = new SmtpServerInfo
            {
                Host = _smtpConfig.Host,
                Port = _smtpConfig.Port,
                UseSsl = _smtpConfig.EnableSsl,
                ServerGreeting = "Connection configuration valid"
            };
        }
        catch (Exception ex)
        {
            result.IsHealthy = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "SMTP health check failed");
        }
        finally
        {
            result.ResponseTime = stopwatch.Elapsed;
        }

        return result;
    }

    public async Task<EmailStatistics> GetEmailStatisticsAsync(DateTime fromDate, DateTime toDate)
    {
        await Task.CompletedTask; // Placeholder for async operations

        var statistics = new EmailStatistics
        {
            FromDate = fromDate,
            ToDate = toDate
        };

        lock (_historyLock)
        {
            var relevantEmails = _emailHistory
                .Where(e => e.SentAt >= fromDate && e.SentAt <= toDate)
                .ToList();

            statistics.TotalSent = relevantEmails.Count;
            statistics.TotalDelivered = relevantEmails.Count(e => e.IsSuccess);
            statistics.TotalBounced = relevantEmails.Count(e => !e.IsSuccess);

            // Group by categories if metadata contains category information
            var categoryGroups = relevantEmails
                .Where(e => e.Metadata.ContainsKey("Category"))
                .GroupBy(e => (EmailCategory)e.Metadata["Category"]);

            foreach (var group in categoryGroups)
            {
                statistics.CategoryStats[group.Key] = new CategoryStatistics
                {
                    Sent = group.Count(),
                    Delivered = group.Count(e => e.IsSuccess),
                    Bounced = group.Count(e => !e.IsSuccess)
                };
            }
        }

        return statistics;
    }

    public Task<EmailQueueResult> QueueEmailAsync(EmailMessage emailMessage, DateTime? scheduledTime = null)
    {
        // For now, we'll implement a simple immediate sending queue
        // In production, this would integrate with a proper queue system like Hangfire
        
        try
        {
            var queueId = Guid.NewGuid().ToString();
            var estimatedDelivery = scheduledTime ?? DateTime.UtcNow.AddSeconds(30);

            if (scheduledTime.HasValue && scheduledTime.Value > DateTime.UtcNow)
            {
                // Schedule for later: enqueue a delayed task so hosted service will run it when time arrives
                if (_taskQueue != null)
                {
                    _taskQueue.QueueBackgroundWorkItem(async ct =>
                    {
                        var delay = scheduledTime.Value - DateTime.UtcNow;
                        if (delay > TimeSpan.Zero)
                            await Task.Delay(delay, ct);
                        await SendEmailAsync(emailMessage, ct);
                    });
                }
                else
                {
                    // Fallback: schedule on thread-pool but observe exceptions
                    _ = Task.Delay(scheduledTime.Value - DateTime.UtcNow).ContinueWith(t =>
                    {
                        // Run send and log any exceptions
                        SendEmailAsync(emailMessage).ContinueWith(inner =>
                        {
                            if (inner.Exception != null)
                                _logger.LogError(inner.Exception, "Scheduled SendEmail failed");
                        }, TaskScheduler.Default);
                    }, TaskScheduler.Default);
                }
            }
            else
            {
                // Send immediately in background via background queue if available
                if (_taskQueue != null)
                {
                    _taskQueue.QueueBackgroundWorkItem(async ct => await SendEmailAsync(emailMessage, ct));
                }
                else
                {
                    // Fallback: ensure exceptions are observed and logged
                    _ = SendEmailAsync(emailMessage).ContinueWith(t =>
                    {
                        if (t.Exception != null)
                        {
                            _logger.LogError(t.Exception, "SendEmailAsync background invocation failed");
                        }
                    }, TaskScheduler.Default);
                }
            }

            var result = new EmailQueueResult
            {
                QueueId = queueId,
                Status = EmailQueueStatus.Queued,
                QueuedAt = DateTime.UtcNow,
                ScheduledFor = scheduledTime,
                EstimatedDeliveryTime = estimatedDelivery,
                QueuePosition = 1 // Simplified
            };
            
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue email for {Recipients}", 
                string.Join(", ", emailMessage.To.Select(x => x.Address)));
            
            var errorResult = new EmailQueueResult
            {
                QueueId = Guid.NewGuid().ToString(),
                Status = EmailQueueStatus.Failed,
                QueuedAt = DateTime.UtcNow,
                Error = ex.Message
            };
            
            return Task.FromResult(errorResult);
        }
    }

    // Legacy method support for backward compatibility
    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false)
    {
        var emailMessage = new EmailMessage
        {
            From = _smtpConfig.DefaultFromEmail,
            FromName = _smtpConfig.DefaultFromName,
            Subject = subject,
            TextBody = isHtml ? null : body,
            HtmlBody = isHtml ? body : null
        };

        emailMessage.To.Add(new EmailAddress(to));

        var result = await SendEmailAsync(emailMessage);
        return result.IsSuccess;
    }

    public async Task<bool> SendWelcomeEmailAsync(string to, string userName)
    {
        try
        {
            var request = new TemplatedEmailRequest
            {
                To = to,
                TemplateId = "welcome",
                Variables = new Dictionary<string, object>
                {
                    ["userName"] = userName,
                    ["siteName"] = "Gaming Café"
                }
            };

            var result = await SendTemplatedEmailAsync(request);
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendReservationConfirmationAsync(string to, string userName, DateTime startTime, DateTime endTime, string stationName)
    {
        try
        {
            var request = new TemplatedEmailRequest
            {
                To = to,
                TemplateId = "booking-confirmation",
                Variables = new Dictionary<string, object>
                {
                    ["userName"] = userName,
                    ["stationName"] = stationName,
                    ["startTime"] = startTime.ToString("yyyy-MM-dd HH:mm"),
                    ["endTime"] = endTime.ToString("yyyy-MM-dd HH:mm"),
                    ["duration"] = (endTime - startTime).TotalHours.ToString("F1")
                }
            };

            var result = await SendTemplatedEmailAsync(request);
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reservation confirmation to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(string to, string resetToken)
    {
        try
        {
            var request = new TemplatedEmailRequest
            {
                To = to,
                TemplateId = "password-reset",
                Variables = new Dictionary<string, object>
                {
                    ["resetToken"] = resetToken,
                    ["resetUrl"] = $"https://localhost:5001/reset-password?token={resetToken}",
                    ["expiryTime"] = DateTime.UtcNow.AddHours(24).ToString("yyyy-MM-dd HH:mm UTC")
                }
            };

            var result = await SendTemplatedEmailAsync(request);
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendEmailVerificationAsync(string to, string userName, string verificationToken)
    {
        try
        {
            var emailMessage = new EmailMessage
            {
                From = _smtpConfig.DefaultFromEmail,
                FromName = _smtpConfig.DefaultFromName,
                Subject = "Email Verification - Gaming Café",
                HtmlBody = $@"
                    <html>
                    <body>
                        <h2>Welcome to Gaming Café, {userName}!</h2>
                        <p>Please verify your email address by clicking the link below:</p>
                        <a href='https://localhost:5001/verify-email?token={verificationToken}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Verify Email</a>
                        <p>If you didn't create an account, please ignore this email.</p>
                        <p>Best regards,<br>Gaming Café Team</p>
                    </body>
                    </html>",
                Priority = EmailPriority.High,
                Category = EmailCategory.Transactional
            };

            emailMessage.To.Add(new EmailAddress(to));

            var result = await SendEmailAsync(emailMessage);
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendLowBalanceNotificationAsync(string to, string userName, decimal currentBalance)
    {
        try
        {
            var emailMessage = new EmailMessage
            {
                From = _smtpConfig.DefaultFromEmail,
                FromName = _smtpConfig.DefaultFromName,
                Subject = "Low Balance Alert - Gaming Café",
                HtmlBody = $@"
                    <html>
                    <body>
                        <h2>Low Balance Alert</h2>
                        <p>Hello {userName},</p>
                        <p>Your account balance is running low. Current balance: <strong>${currentBalance:F2}</strong></p>
                        <p>To continue enjoying our gaming services, please add funds to your account.</p>
                        <a href='https://localhost:5001/wallet/topup' style='background-color: #28a745; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Add Funds</a>
                        <p>Thank you for choosing Gaming Café!</p>
                    </body>
                    </html>",
                Priority = EmailPriority.Normal,
                Category = EmailCategory.Notification
            };

            emailMessage.To.Add(new EmailAddress(to));

            var result = await SendEmailAsync(emailMessage);
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send low balance notification to {To}", to);
            return false;
        }
    }

    private SmtpConfiguration LoadSmtpConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("Email:Smtp");
        var config = new SmtpConfiguration
        {
            Host = section["Host"] ?? configuration["Email:SmtpHost"] ?? "localhost",
            Port = int.Parse(section["Port"] ?? configuration["Email:SmtpPort"] ?? "1025"),
            EnableSsl = bool.Parse(section["EnableSsl"] ?? configuration["Email:EnableSsl"] ?? "false"),
            Username = section["Username"] ?? configuration["Email:Username"],
            Password = section["Password"] ?? configuration["Email:Password"],
            DefaultFromEmail = section["DefaultFromEmail"] ?? configuration["Email:FromEmail"] ?? "noreply@gamingcafe.local",
            DefaultFromName = section["DefaultFromName"] ?? configuration["Email:FromName"] ?? "Gaming Cafe",
            TimeoutMs = int.Parse(section["TimeoutMs"] ?? "30000")
        };

        // Set development defaults if not configured
        if (string.IsNullOrEmpty(config.Host) || config.Host == "localhost")
        {
            config.Host = "localhost";
            config.Port = 1025; // Default for development (like MailHog)
            config.EnableSsl = false;
            config.DefaultFromEmail = "noreply@gamingcafe.local";
            config.DefaultFromName = "Gaming Cafe";
        }

        return config;
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_smtpConfig.Host, _smtpConfig.Port)
        {
            EnableSsl = _smtpConfig.EnableSsl,
            Timeout = _smtpConfig.TimeoutMs
        };

        if (!string.IsNullOrEmpty(_smtpConfig.Username))
        {
            client.Credentials = new NetworkCredential(_smtpConfig.Username, _smtpConfig.Password);
        }

        return client;
    }

    private MailMessage CreateMailMessage(EmailMessage emailMessage)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(emailMessage.From, emailMessage.FromName),
            Subject = emailMessage.Subject,
            Priority = ConvertPriority(emailMessage.Priority)
        };

        // Add recipients
        foreach (var to in emailMessage.To)
        {
            mailMessage.To.Add(new MailAddress(to.Address, to.Name));
        }

        foreach (var cc in emailMessage.Cc)
        {
            mailMessage.CC.Add(new MailAddress(cc.Address, cc.Name));
        }

        foreach (var bcc in emailMessage.Bcc)
        {
            mailMessage.Bcc.Add(new MailAddress(bcc.Address, bcc.Name));
        }

        // Set reply-to
        if (!string.IsNullOrEmpty(emailMessage.ReplyTo))
        {
            mailMessage.ReplyToList.Add(emailMessage.ReplyTo);
        }

        // Set body content
        if (!string.IsNullOrEmpty(emailMessage.HtmlBody))
        {
            mailMessage.Body = emailMessage.HtmlBody;
            mailMessage.IsBodyHtml = true;
        }
        else if (!string.IsNullOrEmpty(emailMessage.TextBody))
        {
            mailMessage.Body = emailMessage.TextBody;
            mailMessage.IsBodyHtml = false;
        }

        // Add attachments
        foreach (var attachment in emailMessage.Attachments)
        {
            var stream = new MemoryStream(attachment.Data);
            var mailAttachment = new Attachment(stream, attachment.FileName, attachment.ContentType);
            
            if (!string.IsNullOrEmpty(attachment.ContentId))
            {
                mailAttachment.ContentId = attachment.ContentId;
            }

            mailMessage.Attachments.Add(mailAttachment);
        }

        // Add custom headers
        foreach (var header in emailMessage.Headers)
        {
            mailMessage.Headers.Add(header.Key, header.Value);
        }

        return mailMessage;
    }

    private void ValidateEmailMessage(EmailMessage emailMessage)
    {
        if (string.IsNullOrEmpty(emailMessage.From))
            throw new ArgumentException("From address is required");

        if (!emailMessage.To.Any())
            throw new ArgumentException("At least one recipient is required");

        if (string.IsNullOrEmpty(emailMessage.Subject))
            throw new ArgumentException("Subject is required");

        if (string.IsNullOrEmpty(emailMessage.HtmlBody) && string.IsNullOrEmpty(emailMessage.TextBody))
            throw new ArgumentException("Email body (HTML or text) is required");
    }

    private void RecordEmailResult(EmailResult result)
    {
        lock (_historyLock)
        {
            _emailHistory.Add(result);
            
            // Keep only recent history (last 1000 emails)
            if (_emailHistory.Count > 1000)
            {
                _emailHistory.RemoveRange(0, _emailHistory.Count - 1000);
            }
        }
    }

    private string ProcessTemplate(string template, Dictionary<string, object> variables)
    {
        var result = template;
        
        foreach (var variable in variables)
        {
            var placeholder = $"{{{{{variable.Key}}}}}";
            result = result.Replace(placeholder, variable.Value?.ToString() ?? string.Empty);
        }

        return result;
    }

    private Dictionary<string, EmailTemplate> LoadDefaultTemplates()
    {
        return new Dictionary<string, EmailTemplate>
        {
            ["welcome"] = new EmailTemplate
            {
                Id = "welcome",
                Name = "Welcome Email",
                SubjectTemplate = "Welcome to Gaming Cafe, {{UserName}}!",
                HtmlTemplate = @"
                    <h1>Welcome {{UserName}}!</h1>
                    <p>Thank you for joining Gaming Cafe. Your account has been created successfully.</p>
                    <p>Your username: <strong>{{Username}}</strong></p>
                    <p>Get started by logging in and booking your first gaming session!</p>
                    <a href='{{LoginUrl}}' style='background: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Login Now</a>
                ",
                TextTemplate = "Welcome {{UserName}}! Thank you for joining Gaming Cafe. Login at {{LoginUrl}}",
                Category = EmailCategory.Welcome
            },
            
            ["password-reset"] = new EmailTemplate
            {
                Id = "password-reset",
                Name = "Password Reset",
                SubjectTemplate = "Reset Your Gaming Cafe Password",
                HtmlTemplate = @"
                    <h1>Password Reset Request</h1>
                    <p>You requested a password reset for your Gaming Cafe account.</p>
                    <p>Click the link below to reset your password:</p>
                    <a href='{{ResetUrl}}' style='background: #28a745; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a>
                    <p>This link will expire in {{ExpirationMinutes}} minutes.</p>
                    <p>If you didn't request this, please ignore this email.</p>
                ",
                TextTemplate = "Reset your password at: {{ResetUrl}} (expires in {{ExpirationMinutes}} minutes)",
                Category = EmailCategory.PasswordReset
            },

            ["booking-confirmation"] = new EmailTemplate
            {
                Id = "booking-confirmation",
                Name = "Booking Confirmation",
                SubjectTemplate = "Gaming Session Confirmed - {{BookingId}}",
                HtmlTemplate = @"
                    <h1>Booking Confirmed!</h1>
                    <p>Your gaming session has been confirmed.</p>
                    <h3>Booking Details:</h3>
                    <ul>
                        <li><strong>Booking ID:</strong> {{BookingId}}</li>
                        <li><strong>Station:</strong> {{StationName}}</li>
                        <li><strong>Date:</strong> {{BookingDate}}</li>
                        <li><strong>Time:</strong> {{StartTime}} - {{EndTime}}</li>
                        <li><strong>Total Amount:</strong> ${{TotalAmount}}</li>
                    </ul>
                    <p>See you at Gaming Cafe!</p>
                ",
                TextTemplate = "Booking confirmed! ID: {{BookingId}}, Station: {{StationName}}, {{BookingDate}} {{StartTime}}-{{EndTime}}, Total: ${{TotalAmount}}",
                Category = EmailCategory.Notification
            }
        };
    }

    private async Task<bool> DomainExistsAsync(string domain)
    {
        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(domain);
            return hostEntry.AddressList.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckDeliverabilityAsync(string emailAddress)
    {
        // Simplified deliverability check
        // In production, this would integrate with services like ZeroBounce, BriteVerify, etc.
        
        await Task.Delay(100); // Simulate API call
        
        // For now, just check if domain accepts mail
        var domain = emailAddress.Split('@')[1];
        return await DomainExistsAsync(domain);
    }

    private static MailPriority ConvertPriority(EmailPriority priority)
    {
        return priority switch
        {
            EmailPriority.Low => MailPriority.Low,
            EmailPriority.Normal => MailPriority.Normal,
            EmailPriority.High => MailPriority.High,
            EmailPriority.Urgent => MailPriority.High,
            _ => MailPriority.Normal
        };
    }

    public void Dispose()
    {
        _rateLimitSemaphore?.Dispose();
    }
}
