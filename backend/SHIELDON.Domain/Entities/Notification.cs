using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// An in-app notification delivered to a specific user.
/// Generated automatically by system events (enrollment, announcements, exam reminders, results).
/// Notifications can be aggregated: multiple normal events → one grouped notification.
/// </summary>
public class Notification
{
    public Guid Id { get; set; }

    // ── Recipient ────────────────────────────────────────────────
    /// <summary>The User who receives this notification.</summary>
    public Guid RecipientUserId { get; set; }

    // ── Content ──────────────────────────────────────────────────
    /// <summary>Short title shown in the notification dropdown (e.g., "Enrollment Approved").</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Full supporting message for the notification.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Event category driving icon and routing behavior on the frontend.</summary>
    public NotificationType Type { get; set; }

    // ── Read Status ──────────────────────────────────────────────
    public bool IsRead { get; set; } = false;

    /// <summary>UTC timestamp when the user marked this notification as read.</summary>
    public DateTime? ReadAt { get; set; }

    // ── Context Links (all optional) ─────────────────────────────
    /// <summary>Related course ID — allows frontend to link directly to the course page.</summary>
    public Guid? RelatedCourseId { get; set; }

    /// <summary>Related exam ID — allows frontend to link directly to the exam page.</summary>
    public Guid? RelatedExamId { get; set; }

    // ── Timestamps ──────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ────────────────────────────────────
    public User? RecipientUser { get; set; }
}
