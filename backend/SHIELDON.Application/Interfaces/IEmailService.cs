namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Contract for sending emails via SMTP.
/// Implemented in SHIELDON.Infrastructure/Services/EmailService.cs.
/// The Application layer depends ONLY on this interface - never on the implementation.
/// </summary>
public interface IEmailService
{
    /// <summary>Sends a verification email with a clickable token link.</summary>
    Task SendEmailVerificationAsync(string toEmail, string toName, string verificationToken);

    /// <summary>Sends a password reset email with a clickable token link.</summary>
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken);

    /// <summary>Sends an account lockout notification email.</summary>
    Task SendAccountLockedEmailAsync(string toEmail, string toName);

    /// <summary>Sends a security notification after a password or email change.</summary>
    Task SendSecurityNotificationEmailAsync(string toEmail, string toName, string eventDescription);

    /// <summary>Sends a generic templated email.</summary>
    Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody);

    /// <summary>Sends a system notification email.</summary>
    Task SendNotificationEmailAsync(string toEmail, string toName, string title, string message, string frontendUrl);
}
