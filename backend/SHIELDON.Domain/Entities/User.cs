using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Represents a registered user in the SHIELDON system.
/// A user can be an Admin, Tutor, or Student.
/// All timestamps are stored in UTC.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    // ── Identity ───────────────────────────────────────────────
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // ── Profile ────────────────────────────────────────────────
    /// <summary>Relative path to uploaded avatar, e.g. "uploads/avatars/guid.jpg"</summary>
    public string? ProfilePictureUrl { get; set; }

    // ── Role & Status ──────────────────────────────────────────
    public UserRole Role { get; set; }
    public AccountStatus AccountStatus { get; set; } = AccountStatus.Unverified;

    public string? StudentId { get; set; }
    public string? TutorId { get; set; }
    public string? AdminId { get; set; }

    // ── Email Verification ─────────────────────────────────────
    /// <summary>Secure verification token sent via email. Null when verified.</summary>
    public string? VerificationCode { get; set; }

    /// <summary>UTC expiry of the verification token. Always validated before use.</summary>
    public DateTime? VerificationCodeExpiresAt { get; set; }


    // ── Password Reset ─────────────────────────────────────────
    /// <summary>Secure reset token sent via email. Null when no reset is in progress.</summary>
    public string? ResetPasswordCode { get; set; }

    /// <summary>UTC expiry of the password reset code. Always validated before use.</summary>
    public DateTime? ResetPasswordCodeExpiresAt { get; set; }

    // ── Security Tracking ──────────────────────────────────────
    /// <summary>Total number of failed login attempts since last successful login.</summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>UTC timestamp of the user's most recent login. Null if never logged in.</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>UTC timestamp of the user's most recent active connection or activity status. Null if never active.</summary>
    public DateTime? LastSeenAt { get; set; }

    /// <summary>UTC timestamp when the account was locked due to too many failed attempts.</summary>
    public DateTime? LockedAt { get; set; }

    /// <summary>UTC timestamp when the user's email was successfully verified.</summary>
    public DateTime? EmailVerifiedAt { get; set; }

    // ── Onboarding ─────────────────────────────────────────────
    /// <summary>Set to true once the user completes or dismisses the first-login Shepherd.js tour.</summary>
    public bool HasCompletedOnboarding { get; set; } = false;

    // ── Timestamps ─────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ──────────────────────────────────
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    // ── Computed ───────────────────────────────────────────────
    public string FullName => $"{FirstName} {LastName}";
}
