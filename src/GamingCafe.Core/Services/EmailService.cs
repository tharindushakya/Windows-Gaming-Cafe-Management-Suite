using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GamingCafe.Core.Interfaces.Services;

namespace GamingCafe.Core.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly SmtpClient _smtpClient;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _smtpClient = CreateSmtpClient();
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        return await SendEmailAsync(new[] { to }, subject, body, isHtml);
    }

    public async Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true)
    {
        try
        {
            var fromAddress = _configuration["Email:FromAddress"];
            var fromName = _configuration["Email:FromName"] ?? "Gaming Cafe";

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromAddress!, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            foreach (var recipient in to)
            {
                mailMessage.To.Add(recipient);
            }

            await _smtpClient.SendMailAsync(mailMessage);
            
            _logger.LogInformation("Email sent successfully to {Recipients} with subject: {Subject}", 
                string.Join(", ", to), subject);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients} with subject: {Subject}", 
                string.Join(", ", to), subject);
            return false;
        }
    }

    public async Task<bool> SendEmailWithAttachmentAsync(string to, string subject, string body, 
        byte[] attachment, string attachmentName, bool isHtml = true)
    {
        try
        {
            var fromAddress = _configuration["Email:FromAddress"];
            var fromName = _configuration["Email:FromName"] ?? "Gaming Cafe";

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromAddress!, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            mailMessage.To.Add(to);

            // Add attachment
            var stream = new MemoryStream(attachment);
            var attachmentObj = new Attachment(stream, attachmentName);
            mailMessage.Attachments.Add(attachmentObj);

            await _smtpClient.SendMailAsync(mailMessage);
            
            _logger.LogInformation("Email with attachment sent successfully to {Recipient} with subject: {Subject}", 
                to, subject);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachment to {Recipient} with subject: {Subject}", 
                to, subject);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(string to, string resetLink)
    {
        var subject = "Reset Your Gaming Cafe Password";
        var body = GetPasswordResetEmailTemplate(resetLink);
        
        return await SendEmailAsync(to, subject, body);
    }

    public async Task<bool> SendEmailConfirmationAsync(string to, string confirmationLink)
    {
        var subject = "Confirm Your Gaming Cafe Account";
        var body = GetEmailConfirmationTemplate(confirmationLink);
        
        return await SendEmailAsync(to, subject, body);
    }

    public async Task<bool> SendWelcomeEmailAsync(string to, string userName)
    {
        var subject = "Welcome to Gaming Cafe!";
        var body = GetWelcomeEmailTemplate(userName);
        
        return await SendEmailAsync(to, subject, body);
    }

    public async Task<bool> SendSessionReminderAsync(string to, string userName, DateTime sessionTime)
    {
        var subject = "Gaming Session Reminder";
        var body = GetSessionReminderTemplate(userName, sessionTime);
        
        return await SendEmailAsync(to, subject, body);
    }

    public async Task<bool> SendLowStockAlertAsync(string to, IEnumerable<string> lowStockItems)
    {
        var subject = "Low Stock Alert - Gaming Cafe";
        var body = GetLowStockAlertTemplate(lowStockItems);
        
        return await SendEmailAsync(to, subject, body);
    }

    private SmtpClient CreateSmtpClient()
    {
        var host = _configuration["Email:Smtp:Host"];
        var port = int.Parse(_configuration["Email:Smtp:Port"] ?? "587");
        var enableSsl = bool.Parse(_configuration["Email:Smtp:EnableSsl"] ?? "true");
        var username = _configuration["Email:Smtp:Username"];
        var password = _configuration["Email:Smtp:Password"];

        var smtpClient = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(username, password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000 // 30 seconds
        };

        return smtpClient;
    }

    private string GetPasswordResetEmailTemplate(string resetLink)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <title>Password Reset</title>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
                .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 5px; }}
                .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                .content {{ padding: 20px; }}
                .button {{ display: inline-block; background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
                .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>Password Reset Request</h1>
                </div>
                <div class='content'>
                    <p>You have requested to reset your password for your Gaming Cafe account.</p>
                    <p>Click the button below to reset your password:</p>
                    <a href='{resetLink}' class='button'>Reset Password</a>
                    <p>If you didn't request this password reset, please ignore this email.</p>
                    <p>This link will expire in 24 hours for security reasons.</p>
                </div>
                <div class='footer'>
                    <p>Best regards,<br>Gaming Cafe Team</p>
                </div>
            </div>
        </body>
        </html>";
    }

    private string GetEmailConfirmationTemplate(string confirmationLink)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <title>Email Confirmation</title>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
                .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 5px; }}
                .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                .content {{ padding: 20px; }}
                .button {{ display: inline-block; background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
                .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>Confirm Your Email</h1>
                </div>
                <div class='content'>
                    <p>Thank you for registering with Gaming Cafe!</p>
                    <p>Please confirm your email address by clicking the button below:</p>
                    <a href='{confirmationLink}' class='button'>Confirm Email</a>
                    <p>If you didn't create this account, please ignore this email.</p>
                </div>
                <div class='footer'>
                    <p>Welcome to Gaming Cafe!<br>Gaming Cafe Team</p>
                </div>
            </div>
        </body>
        </html>";
    }

    private string GetWelcomeEmailTemplate(string userName)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <title>Welcome to Gaming Cafe</title>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
                .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 5px; }}
                .header {{ background-color: #6f42c1; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                .content {{ padding: 20px; }}
                .feature {{ background-color: #f8f9fa; padding: 15px; margin: 10px 0; border-radius: 4px; }}
                .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>Welcome to Gaming Cafe!</h1>
                </div>
                <div class='content'>
                    <p>Hi {userName},</p>
                    <p>Welcome to Gaming Cafe! We're excited to have you join our gaming community.</p>
                    
                    <h3>What you can do:</h3>
                    <div class='feature'>
                        <strong>🎮 Book Gaming Sessions</strong><br>
                        Reserve your favorite gaming stations in advance
                    </div>
                    <div class='feature'>
                        <strong>💰 Manage Your Wallet</strong><br>
                        Add funds and track your spending
                    </div>
                    <div class='feature'>
                        <strong>🏆 Earn Loyalty Points</strong><br>
                        Get rewards for your gaming activities
                    </div>
                    <div class='feature'>
                        <strong>🛒 POS Shopping</strong><br>
                        Purchase snacks, drinks, and gaming accessories
                    </div>
                    
                    <p>Start your gaming journey today!</p>
                </div>
                <div class='footer'>
                    <p>Happy Gaming!<br>Gaming Cafe Team</p>
                </div>
            </div>
        </body>
        </html>";
    }

    private string GetSessionReminderTemplate(string userName, DateTime sessionTime)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <title>Gaming Session Reminder</title>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
                .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 5px; }}
                .header {{ background-color: #17a2b8; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                .content {{ padding: 20px; }}
                .highlight {{ background-color: #e7f3ff; padding: 15px; border-radius: 4px; margin: 15px 0; }}
                .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>Gaming Session Reminder</h1>
                </div>
                <div class='content'>
                    <p>Hi {userName},</p>
                    <p>This is a reminder about your upcoming gaming session:</p>
                    
                    <div class='highlight'>
                        <strong>Session Time:</strong> {sessionTime:dddd, MMMM dd, yyyy 'at' h:mm tt}
                    </div>
                    
                    <p>Please arrive a few minutes early to ensure you get the full duration of your session.</p>
                    <p>If you need to cancel or reschedule, please contact us as soon as possible.</p>
                </div>
                <div class='footer'>
                    <p>See you soon!<br>Gaming Cafe Team</p>
                </div>
            </div>
        </body>
        </html>";
    }

    private string GetLowStockAlertTemplate(IEnumerable<string> lowStockItems)
    {
        var itemsList = string.Join("</li><li>", lowStockItems);
        
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <title>Low Stock Alert</title>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
                .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 5px; }}
                .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                .content {{ padding: 20px; }}
                .alert {{ background-color: #f8d7da; border: 1px solid #f5c6cb; color: #721c24; padding: 15px; border-radius: 4px; margin: 15px 0; }}
                .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>⚠️ Low Stock Alert</h1>
                </div>
                <div class='content'>
                    <div class='alert'>
                        <strong>Attention:</strong> The following items are running low in stock and need to be restocked:
                    </div>
                    
                    <ul>
                        <li>{itemsList}</li>
                    </ul>
                    
                    <p>Please reorder these items as soon as possible to avoid running out of stock.</p>
                </div>
                <div class='footer'>
                    <p>Gaming Cafe Inventory System</p>
                </div>
            </div>
        </body>
        </html>";
    }

    public void Dispose()
    {
        _smtpClient?.Dispose();
    }
}
