using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// An in-app notification delivered to a specific user.
/// Generated automatically by system events (enrollment, announcements, exam reminders, results).
/// Notifications can be aggregated: rapid successive events → one grouped notification.
/// </summary>
public class Notification
{
    public Guid Id { get; set; }

    // ── Recipient ────────────────────────────────────────────────
    /// <summary>The User who receives this notification.</summary>
    public Guid UserId { get; set; }

    // ── Content ──────────────────────────────────────────────────
    /// <summary>Short title shown in the notification dropdown (e.g., "Enrollment Approved").</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Full supporting message for the notification.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>URL to redirect the user to when clicked.</summary>
    public string? ActionUrl { get; set; }

    /// <summary>Event category driving icon and routing behavior on the frontend.</summary>
    public NotificationType Type { get; set; }

    // ── Read Status ──────────────────────────────────────────────
    public bool IsRead { get; set; } = false;

    // ── Context Links ────────────────────────────────────────────
    /// <summary>Tracks the exact entity that triggered the event to support aggregation logic.</summary>
    public Guid? RelatedEntityId { get; set; }

    // ── Timestamps ──────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ────────────────────────────────────
    public User? User { get; set; }
}
