namespace SHIELDON.Domain.Enums;

/// <summary>
/// Represents the phone number verification state of a user.
/// </summary>
public enum PhoneVerificationStatus
{
    /// <summary>No phone number has been provided.</summary>
    None,

    /// <summary>A phone number has been entered, but not yet verified via OTP.</summary>
    Unverified,

    /// <summary>The phone number was successfully verified via WhatsApp OTP.</summary>
    Verified = 2
}
