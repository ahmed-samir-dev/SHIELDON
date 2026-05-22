using SHIELDON.Domain.Enums;

namespace SHIELDON.Application.Features.Users.DTOs;

/// <summary>
/// Fully-detailed view of a single user for Admin management purposes.
/// Exposes all non-sensitive user fields. Sensitive fields (password hash,
/// verification/reset tokens) are never included.
/// Admins are never included in this DTO's context.
/// </summary>
public class UserDetailDto
{
    // ── Identity ────────────────────────────────────────────────
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;

    // ── Profile ─────────────────────────────────────────────────
    public string? ProfilePictureUrl { get; set; }

    // ── Role & IDs ──────────────────────────────────────────────
    public UserRole Role { get; set; }

    /// <summary>Role-specific display ID (e.g., "STU-2026-A1B2"). Populated after first login.</summary>
    public string? StudentId { get; set; }

    /// <summary>Role-specific display ID (e.g., "TUT-2026-C3D4"). Populated after first login.</summary>
    public string? TutorId { get; set; }

    // ── Account Status ──────────────────────────────────────────
    public AccountStatus AccountStatus { get; set; }

    /// <summary>Number of consecutive failed login attempts since last successful login.</summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>UTC timestamp when the account was locked. Null if not currently locked.</summary>
    public DateTime? LockedAt { get; set; }

    // ── Email Verification ──────────────────────────────────────
    /// <summary>UTC timestamp when the user verified their email. Null if not yet verified.</summary>
    public DateTime? EmailVerifiedAt { get; set; }

    // ── Login Activity ──────────────────────────────────────────
    /// <summary>UTC timestamp of the user's most recent successful login. Null if never logged in.</summary>
    public DateTime? LastLoginAt { get; set; }

    // ── Onboarding ──────────────────────────────────────────────
    /// <summary>True once the user completes or dismisses the first-login onboarding tour.</summary>
    public bool HasCompletedOnboarding { get; set; }

    // ── Timestamps ──────────────────────────────────────────────
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
