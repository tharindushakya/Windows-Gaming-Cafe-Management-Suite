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

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"] ?? "localhost";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var username = _configuration["Email:Username"] ?? "";
            var password = _configuration["Email:Password"] ?? "";
            var fromEmail = _configuration["Email:FromEmail"] ?? "noreply@gamingcafe.com";
            var fromName = _configuration["Email:FromName"] ?? "Gaming Cafe";

            using var client = new SmtpClient(smtpHost, smtpPort);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(username, password);

            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail, fromName);
            message.To.Add(to);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isHtml;
            message.BodyEncoding = Encoding.UTF8;

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendWelcomeEmailAsync(string to, string userName)
    {
        var subject = "Welcome to Gaming Cafe!";
        var body = $@"
            <h2>Welcome to Gaming Cafe, {userName}!</h2>
            <p>Thank you for registering with us. We're excited to have you as part of our gaming community.</p>
            <p>You can now:</p>
            <ul>
                <li>Book gaming stations</li>
                <li>Track your game time</li>
                <li>Manage your wallet</li>
                <li>Join our loyalty program</li>
            </ul>
            <p>Happy Gaming!</p>
            <p>The Gaming Cafe Team</p>
        ";

        return await SendEmailAsync(to, subject, body, true);
    }

    public async Task<bool> SendReservationConfirmationAsync(string to, string userName, DateTime startTime, DateTime endTime, string stationName)
    {
        var subject = "Reservation Confirmed";
        var body = $@"
            <h2>Reservation Confirmed</h2>
            <p>Hi {userName},</p>
            <p>Your reservation has been confirmed:</p>
            <ul>
                <li><strong>Station:</strong> {stationName}</li>
                <li><strong>Start Time:</strong> {startTime:yyyy-MM-dd HH:mm}</li>
                <li><strong>End Time:</strong> {endTime:yyyy-MM-dd HH:mm}</li>
            </ul>
            <p>Please arrive on time to secure your station.</p>
            <p>Thanks,<br>Gaming Cafe Team</p>
        ";

        return await SendEmailAsync(to, subject, body, true);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string to, string resetToken)
    {
        var subject = "Password Reset Request";
        var body = $@"
            <h2>Password Reset Request</h2>
            <p>You have requested to reset your password.</p>
            <p>Your reset token is: <strong>{resetToken}</strong></p>
            <p>This token will expire in 24 hours.</p>
            <p>If you did not request this reset, please ignore this email.</p>
            <p>Gaming Cafe Team</p>
        ";

        return await SendEmailAsync(to, subject, body, true);
    }

    public async Task<bool> SendLowBalanceNotificationAsync(string to, string userName, decimal currentBalance)
    {
        var subject = "Low Wallet Balance";
        var body = $@"
            <h2>Low Wallet Balance</h2>
            <p>Hi {userName},</p>
            <p>Your wallet balance is running low: <strong>${currentBalance:F2}</strong></p>
            <p>Please top up your wallet to continue enjoying our gaming services.</p>
            <p>Thanks,<br>Gaming Cafe Team</p>
        ";

        return await SendEmailAsync(to, subject, body, true);
    }
}
