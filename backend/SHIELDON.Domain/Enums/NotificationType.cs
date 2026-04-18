namespace SHIELDON.Domain.Enums;

/// <summary>
/// Categorizes the event that triggered a notification.
/// Used to drive routing and icon selection on the frontend.
/// </summary>
public enum NotificationType
{
    // ── Announcements ───────────────────────────────────────
    AnnouncementCreated = 0,
    AnnouncementUpdated = 1,

    // ── Enrollment ──────────────────────────────────────────
    EnrollmentApproved = 10,
    EnrollmentRejected = 11,

    // ── Materials ───────────────────────────────────────────
    MaterialUploaded = 20,

    // ── Exams ───────────────────────────────────────────────
    ExamCreated = 30,
    ExamUpdated = 31,
    ExamReminder24h = 32,
    ExamReminder1h = 33,

    // ── Results ─────────────────────────────────────────────
    ResultReleased = 40,

    // ── Re-Attempt Requests ──────────────────────────────────
    ReAttemptApproved = 50,
    ReAttemptRejected = 51
}
