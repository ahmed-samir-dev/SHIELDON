namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Contract for delivering One-Time Password codes via WhatsApp.
/// Code generation, hashing, storage, and verification are handled by ProfileService.
/// This service is responsible solely for message delivery to the user's WhatsApp.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Sends a pre-generated 6-digit OTP code to the recipient's WhatsApp number.
    /// </summary>
    /// <param name="phoneNumber">Recipient's full E.164 phone number (e.g. +201012345678)</param>
    /// <param name="code">The plaintext 6-digit OTP code to include in the WhatsApp message</param>
    /// <param name="ct">Cancellation token</param>
    Task SendOtpAsync(string phoneNumber, string code, CancellationToken ct = default);
}
