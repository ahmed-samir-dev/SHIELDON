namespace SHIELDON.Domain.Enums;

/// <summary>
/// The current status of a user account.
/// Controls whether the user can log in and access features.
/// </summary>
public enum AccountStatus
{
    /// <summary>Account is fully active and can log in.</summary>
    Active,
    /// <summary>Account was created but email has not been verified yet.</summary>
    Unverified,
    /// <summary>Account was locked after too many failed login attempts. Reset password to unlock.</summary>
    Locked,
    /// <summary>Account was disabled by an Admin. User cannot log in.</summary>
    Disabled
}
