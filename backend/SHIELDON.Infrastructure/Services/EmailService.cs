using SHIELDON.Application.Interfaces;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Sends transactional emails via SMTP using MailKit.
/// In development: configured to use Mailtrap.io sandbox (no real emails sent).
/// In production: configured with real SMTP credentials.
/// Credentials are NEVER committed to git — loaded from appsettings.Development.json or User Secrets.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    // Loaded from configuration — see appsettings.json structure
    private string SmtpHost => _configuration["EmailSettings:SmtpHost"] ?? string.Empty;
    private int SmtpPort => int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
    private string SmtpUser => _configuration["EmailSettings:SmtpUser"] ?? string.Empty;
    private string SmtpPassword => _configuration["EmailSettings:SmtpPassword"] ?? string.Empty;
    private string FromName => _configuration["EmailSettings:FromName"] ?? "SHIELDON Platform";
    private string FromEmail => _configuration["EmailSettings:FromEmail"] ?? "noreply@shieldon.com";

    // The publicly accessible URL of the frontend app (used to build verification links)
    private string FrontendUrl => _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:4200";

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendEmailVerificationAsync(string toEmail, string toName, string verificationToken)
    {
        var verifyUrl = $"{FrontendUrl}/auth/verify-email?token={verificationToken}";
        var subject = "Verify Your SHIELDON Account";
        var htmlBody = BuildVerificationEmailHtml(toName, verifyUrl);
        await SendEmailAsync(toEmail, toName, subject, htmlBody);
    }

    /// <inheritdoc />
    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken)
    {
        var resetUrl = $"{FrontendUrl}/auth/reset-password?token={resetToken}";
        var subject = "Reset Your SHIELDON Password";
        var htmlBody = BuildPasswordResetEmailHtml(toName, resetUrl);
        await SendEmailAsync(toEmail, toName, subject, htmlBody);
    }

    /// <inheritdoc />
    public async Task SendAccountLockedEmailAsync(string toEmail, string toName)
    {
        var subject = "Your SHIELDON Account Has Been Locked";
        var htmlBody = BuildAccountLockedEmailHtml(toName);
        await SendEmailAsync(toEmail, toName, subject, htmlBody);
    }

    /// <inheritdoc/>
    public async Task SendSecurityNotificationEmailAsync(string toEmail, string toName, string eventDescription)
    {
        var subject = "SHIELDON Security Notification";
        var htmlBody = BuildSecurityNotificationEmailHtml(toName, eventDescription);
        await SendEmailAsync(toEmail, toName, subject, htmlBody);
    }

    /// <inheritdoc />
    public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(FromName, FromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(SmtpHost, SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(SmtpUser, SmtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {Email} with subject '{Subject}'", toEmail, subject);
        }
        catch (Exception ex)
        {
            // Log the error but do NOT throw — email failure should not crash the API
            _logger.LogError(ex, "Failed to send email to {Email} with subject '{Subject}'", toEmail, subject);
        }
    }

    // ── HTML Email Templates ─────────────────────────────────────────────

    private static string BuildVerificationEmailHtml(string name, string verifyUrl) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family: 'Inter', 'Segoe UI', sans-serif; background: #EDF0F1; padding: 40px;">
          <div style="max-width: 560px; margin: 0 auto; background: #fff; border-radius: 10px; padding: 40px; box-shadow: 0 4px 12px rgba(0,0,0,0.10);">
            <h1 style="font-size: 28px; font-weight: 700; margin: 0 0 8px;">
              <span style="background: linear-gradient(90deg, #215DAE, #1898A1); -webkit-background-clip: text; -webkit-text-fill-color: transparent;">SHIELDON</span>
            </h1>
            <p style="color: #5E6E7A; font-size: 13px; margin: 0 0 32px;">Integrity You Can Trust</p>
            <h2 style="font-size: 22px; color: #0B315B; margin: 0 0 16px;">Verify Your Email Address</h2>
            <p style="color: #5E6E7A; font-size: 15px; line-height: 1.6;">Hello {name},</p>
            <p style="color: #5E6E7A; font-size: 15px; line-height: 1.6;">Thank you for creating a SHIELDON account. Please click the button below to verify your email address and activate your account.</p>
            <div style="text-align: center; margin: 32px 0;">
              <a href="{verifyUrl}" style="display: inline-block; background: linear-gradient(90deg, #215DAE, #1898A1); color: #fff; font-size: 16px; font-weight: 600; padding: 14px 32px; border-radius: 10px; text-decoration: none;">Verify My Email</a>
            </div>
            <p style="color: #87949C; font-size: 13px; line-height: 1.5;">This link expires in <strong>24 hours</strong>. If you didn't create this account, you can safely ignore this email.</p>
          </div>
        </body>
        </html>
        """;

    private static string BuildPasswordResetEmailHtml(string name, string resetUrl) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family: 'Inter', 'Segoe UI', sans-serif; background: #EDF0F1; padding: 40px;">
          <div style="max-width: 560px; margin: 0 auto; background: #fff; border-radius: 10px; padding: 40px; box-shadow: 0 4px 12px rgba(0,0,0,0.10);">
            <h1 style="font-size: 28px; font-weight: 700; margin: 0 0 8px;">
              <span style="background: linear-gradient(90deg, #215DAE, #1898A1); -webkit-background-clip: text; -webkit-text-fill-color: transparent;">SHIELDON</span>
            </h1>
            <p style="color: #5E6E7A; font-size: 13px; margin: 0 0 32px;">Integrity You Can Trust</p>
            <h2 style="font-size: 22px; color: #0B315B; margin: 0 0 16px;">Reset Your Password</h2>
            <p style="color: #5E6E7A; font-size: 15px; line-height: 1.6;">Hello {name},</p>
            <p style="color: #5E6E7A; font-size: 15px; line-height: 1.6;">We received a request to reset your SHIELDON password. Click the button below to create a new password.</p>
            <div style="text-align: center; margin: 32px 0;">
              <a href="{resetUrl}" style="display: inline-block; background: linear-gradient(90deg, #215DAE, #1898A1); color: #fff; font-size: 16px; font-weight: 600; padding: 14px 32px; border-radius: 10px; text-decoration: none;">Reset My Password</a>
            </div>
            <p style="color: #87949C; font-size: 13px; line-height: 1.5;">This link expires in <strong>1 hour</strong>. If you didn't request a password reset, you can safely ignore this email.</p>
          </div>
        </body>
        </html>
        """;

    private static string BuildAccountLockedEmailHtml(string name) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family: 'Inter', 'Segoe UI', sans-serif; background: #EDF0F1; padding: 40px;">
          <div style="max-width: 560px; margin: 0 auto; background: #fff; border-radius: 10px; padding: 40px; box-shadow: 0 4px 12px rgba(0,0,0,0.10);">
            <h1 style="font-size: 28px; font-weight: 700; margin: 0 0 8px;">
              <span style="background: linear-gradient(90deg, #215DAE, #1898A1); -webkit-background-clip: text; -webkit-text-fill-color: transparent;">SHIELDON</span>
            </h1>
            <p style="color: #5E6E7A; font-size: 13px; margin: 0 0 32px;">Integrity You Can Trust</p>
            <h2 style="font-size: 22px; color: #EF4444; margin: 0 0 16px;">Account Locked</h2>
            <p style="color: #5E6E7A; font-size: 15px; line-height: 1.6;">Hello {name},</p>
            <p style="color: #5E6E7A; font-size: 15px; line-height: 1.6;">Your SHIELDON account has been temporarily locked due to 5 failed login attempts. To unlock your account, please reset your password.</p>
            <p style="color: #87949C; font-size: 13px;">If this wasn't you, your account may be targeted. Please reset your password immediately.</p>
          </div>
        </body>
        </html>
        """;

    private static string BuildSecurityNotificationEmailHtml(string name, string eventDescription) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family: 'Inter', 'Segoe UI', sans-serif; background: #EDF0F1; padding: 40px;">
          <div style="max-width: 560px; margin: 0 auto; background: #fff; border-radius: 10px; padding: 40px; box-shadow: 0 4px 12px rgba(0,0,0,0.10);">
            <h1 style="font-size: 28px; font-weight: 700; margin: 0 0 8px;">
              <span style="background: linear-gradient(90deg, #215DAE, #1898A1); -webkit-background-clip: text; -webkit-text-fill-color: transparent;">SHIELDON</span>
            </h1>
            <p style="color: #5E6E7A; font-size: 13px; margin: 0 0 32px;">Integrity You Can Trust</p>
            <h2 style="font-size: 22px; color: #0B315B; margin: 0 0 16px;">Security Notification</h2>
            <p style="color: #5E6E7A; font-size: 15px; line-height: 1.6;">Hello {name},</p>
            <p style="color: #5E6E7A; font-size: 15px; line-height: 1.6;">A security event occurred on your account: <strong>{eventDescription}</strong></p>
            <p style="color: #87949C; font-size: 13px;">If you did not perform this action, please contact support immediately.</p>
          </div>
        </body>
        </html>
        """;
}
