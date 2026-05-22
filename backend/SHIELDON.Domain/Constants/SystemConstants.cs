namespace SHIELDON.Domain.Constants;

/// <summary>
/// System-wide constants for SHIELDON.
/// Never use magic numbers or strings - always reference these constants.
/// </summary>
public static class SystemConstants
{
    // ── Authentication ─────────────────────────────────────────────
    /// <summary>Maximum failed login attempts before account lockout.</summary>
    public const int MAX_LOGIN_ATTEMPTS = 5;

    /// <summary>JWT Access Token expiry in minutes.</summary>
    public const int ACCESS_TOKEN_EXPIRY_MINUTES = 15;

    /// <summary>Refresh Token expiry in days.</summary>
    public const int REFRESH_TOKEN_EXPIRY_DAYS = 7;

    /// <summary>Email verification token expiry in hours.</summary>
    public const int EMAIL_VERIFY_TOKEN_EXPIRY_HOURS = 24;

    /// <summary>Password reset token expiry in hours.</summary>
    public const int PASSWORD_RESET_TOKEN_EXPIRY_HOURS = 1;

    // ── File Upload ────────────────────────────────────────────────
    /// <summary>Maximum course material file size in bytes (20 MB).</summary>
    public const long MAX_MATERIAL_FILE_SIZE_BYTES = 20 * 1024 * 1024;

    /// <summary>Maximum profile picture size in bytes (2 MB).</summary>
    public const long MAX_PROFILE_PICTURE_SIZE_BYTES = 2 * 1024 * 1024;

    // ── Enrollment ─────────────────────────────────────────────────
    /// <summary>Consecutive rejections before a 24-hour cooldown is applied.</summary>
    public const int ENROLLMENT_COOLDOWN_THRESHOLD = 2;

    /// <summary>Cooldown duration in hours after rejection threshold.</summary>
    public const int ENROLLMENT_COOLDOWN_HOURS = 24;

    /// <summary>Maximum total rejections before student is permanently blocked from a course.</summary>
    public const int ENROLLMENT_MAX_REJECTIONS = 3;

    // ── Anti-Cheat ─────────────────────────────────────────────────
    /// <summary>Default maximum violations before force-submit is triggered.</summary>
    public const int DEFAULT_MAX_VIOLATIONS = 3;

    /// <summary>Cooldown in seconds between duplicate violation logs (same type).</summary>
    public const int VIOLATION_COOLDOWN_SECONDS = 3;

    /// <summary>Heartbeat interval in seconds. Frontend sends every 30s.</summary>
    public const int HEARTBEAT_INTERVAL_SECONDS = 30;

    /// <summary>Seconds without heartbeat before session is marked Disconnected.</summary>
    public const int HEARTBEAT_TIMEOUT_SECONDS = 90;

    // ── Notifications ──────────────────────────────────────────────
    /// <summary>Window (in minutes) for grouping identical notification types.</summary>
    public const int NOTIFICATION_AGGREGATION_WINDOW_MINUTES = 5;

    /// <summary>Default page size for paginated list endpoints.</summary>
    public const int DEFAULT_PAGE_SIZE = 20;

    // ── Exam Reminders ─────────────────────────────────────────────
    /// <summary>Time buffer in minutes around the 24-hour reminder window.</summary>
    public const int REMINDER_24H_BUFFER_MINUTES = 5;

    /// <summary>Time buffer in minutes around the 1-hour reminder window.</summary>
    public const int REMINDER_1H_BUFFER_MINUTES = 5;
}
