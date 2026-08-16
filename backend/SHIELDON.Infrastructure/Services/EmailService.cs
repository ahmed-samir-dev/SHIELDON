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
/// Credentials are NEVER committed to git - loaded from appsettings.Development.json or User Secrets.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    // Loaded from configuration - see appsettings.json structure
    private string SmtpHost => _configuration["EmailSettings:SmtpHost"] ?? string.Empty;
    private int SmtpPort => int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
    private string SmtpUser => _configuration["EmailSettings:SmtpUser"] ?? string.Empty;
    private string SmtpPassword => _configuration["EmailSettings:SmtpPassword"] ?? string.Empty;
    private string FromName => _configuration["EmailSettings:FromName"] ?? "SHIELDON Platform";
    private string FromEmail => _configuration["EmailSettings:FromEmail"] ?? "noreply@shieldon.com";

    // The publicly accessible URL of the frontend app (used to build verification links)
    private string FrontendUrl => _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:4201";

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendEmailVerificationAsync(string toEmail, string toName, string verificationToken)
    {
        var encodedEmail = Uri.EscapeDataString(toEmail);
        var verifyUrl = $"{FrontendUrl}/auth/verify-email?email={encodedEmail}&token={verificationToken}";
        var subject = "Verify Your SHIELDON Account";
        var htmlBody = BuildVerificationEmailHtml(toName, verifyUrl);
        await SendEmailAsync(toEmail, toName, subject, htmlBody);
    }

    /// <inheritdoc />
    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken)
    {
        var encodedEmail = Uri.EscapeDataString(toEmail);
        var resetUrl = $"{FrontendUrl}/auth/reset-password?email={encodedEmail}&token={resetToken}";
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
            // Log the error but do NOT throw - email failure should not crash the API
            _logger.LogError(ex, "Failed to send email to {Email} with subject '{Subject}'", toEmail, subject);
        }
    }

    // ── HTML Email Templates ─────────────────────────────────────────────

    public async Task SendNotificationEmailAsync(string toEmail, string toName, string title, string message, string frontendUrl)
    {
        var subject = $"SHIELDON: {title}";
        var innerHtml = $@"
            <h2 style=""font-size: 22px; color: #1E293B; margin: 0 0 16px;"">{title}</h2>
            <p style=""color: #334155; font-size: 15px; line-height: 1.6;"">Hello {toName},</p>
            <p style=""color: #334155; font-size: 15px; line-height: 1.6;"">{message}</p>
            <div style=""text-align: center; margin: 32px 0;"">
              <a href=""{frontendUrl}"" style=""display: inline-block; background-color: #0D9488; color: #fff; font-size: 16px; font-weight: 600; padding: 14px 32px; border-radius: 8px; text-decoration: none;"">View Dashboard</a>
            </div>
            <p style=""color: #64748B; font-size: 13px;"">This is an automated system notification from SHIELDON LMS.</p>
        ";
        var htmlBody = BuildMasterEmailTemplate(innerHtml);
        await SendEmailAsync(toEmail, toName, subject, htmlBody);
    }

    // ── HTML Email Templates ─────────────────────────────────────────────

    private static string BuildMasterEmailTemplate(string innerContent) => $"""
        <!DOCTYPE html>
        <html>
        <head>
          <meta charset="utf-8">
        </head>
        <body style="font-family: 'Inter', 'Helvetica Neue', Helvetica, Arial, sans-serif; background-color: #F8FAFC; padding: 40px 20px; margin: 0;">
          <div style="max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05); border: 1px solid #E2E8F0;">
            <!-- Header -->
            <div style="background-color: #1E293B; padding: 30px 20px; text-align: center;">
                <h1 style="color: #ffffff; margin: 0; font-size: 26px; font-weight: 700; letter-spacing: -0.5px;">
                  SHIELD<span style="color: #0D9488;">ON</span>
                </h1>
                <p style="color: #94A3B8; margin: 8px 0 0 0; font-size: 14px; letter-spacing: 0.5px; text-transform: uppercase;">Integrity You Can Trust</p>
            </div>
            
            <!-- Body Content -->
            <div style="padding: 40px 30px; background-color: #ffffff;">
                {innerContent}
            </div>
            
            <!-- Footer -->
            <div style="background-color: #F1F5F9; padding: 24px; text-align: center; border-top: 1px solid #E2E8F0;">
                <p style="margin: 0; color: #64748B; font-size: 13px;">&copy; {DateTime.UtcNow.Year} SHIELDON. All rights reserved.</p>
                <p style="margin: 6px 0 0 0; color: #64748B; font-size: 12px;">Please do not reply directly to this email.</p>
            </div>
          </div>
        </body>
        </html>
        """;

    private static string BuildVerificationEmailHtml(string name, string verifyUrl)
    {
        var content = $@"
            <h2 style=""font-size: 22px; color: #1E293B; margin: 0 0 16px;"">Verify Your Email Address</h2>
            <p style=""color: #334155; font-size: 15px; line-height: 1.6;"">Hello {name},</p>
            <p style=""color: #334155; font-size: 15px; line-height: 1.6;"">Thank you for creating a SHIELDON account. Please click the button below to verify your email address and activate your account.</p>
            <div style=""text-align: center; margin: 32px 0;"">
              <a href=""{verifyUrl}"" style=""display: inline-block; background-color: #0D9488; color: #ffffff; font-size: 16px; font-weight: 600; padding: 14px 32px; border-radius: 8px; text-decoration: none;"">Verify My Email</a>
            </div>
            <p style=""color: #64748B; font-size: 13px; line-height: 1.5;"">This link expires in <strong>24 hours</strong>. If you didn't create this account, you can safely ignore this email.</p>
        ";
        return BuildMasterEmailTemplate(content);
    }

    private static string BuildPasswordResetEmailHtml(string name, string resetUrl)
    {
        var content = $@"
            <h2 style=""font-size: 22px; color: #1E293B; margin: 0 0 16px;"">Reset Your Password</h2>
            <p style=""color: #334155; font-size: 15px; line-height: 1.6;"">Hello {name},</p>
            <p style=""color: #334155; font-size: 15px; line-height: 1.6;"">We received a request to reset your SHIELDON password. Click the button below to create a new password.</p>
            <div style=""text-align: center; margin: 32px 0;"">
              <a href=""{resetUrl}"" style=""display: inline-block; background-color: #0D9488; color: #ffffff; font-size: 16px; font-weight: 600; padding: 14px 32px; border-radius: 8px; text-decoration: none;"">Reset My Password</a>
            </div>
            <p style=""color: #64748B; font-size: 13px; line-height: 1.5;"">This link expires in <strong>1 hour</strong>. If you didn't request a password reset, you can safely ignore this email.</p>
        ";
        return BuildMasterEmailTemplate(content);
    }

    private static string BuildAccountLockedEmailHtml(string name)
    {
        var content = $@"
            <h2 style=""font-size: 22px; color: #DC2626; margin: 0 0 16px;"">Account Locked</h2>
            <p style=""color: #334155; font-size: 15px; line-height: 1.6;"">Hello {name},</p>
            <p style=""color: #334155; font-size: 15px; line-height: 1.6;"">Your SHIELDON account has been temporarily locked due to 5 failed login attempts. To unlock your account, please reset your password.</p>
            <p style=""color: #64748B; font-size: 13px;"">If this wasn't you, your account may be targeted. Please reset your password immediately.</p>
        ";
        return BuildMasterEmailTemplate(content);
    }

    private static string BuildSecurityNotificationEmailHtml(string name, string eventDescription)
    {
        var content = $@"
            <h2 style=""font-size: 22px; color: #1E293B; margin: 0 0 16px;"">Security Notification</h2>
            <p style=""color: #334155; font-size: 15px; line-height: 1.6;"">Hello {name},</p>
            <p style=""color: #334155; font-size: 15px; line-height: 1.6;"">A security event occurred on your account: <strong>{eventDescription}</strong></p>
            <p style=""color: #64748B; font-size: 13px;"">If you did not perform this action, please contact support immediately.</p>
        ";
        return BuildMasterEmailTemplate(content);
    }
}
