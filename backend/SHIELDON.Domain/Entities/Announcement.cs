using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Common;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// A course-level announcement posted by a Tutor or Admin.
/// Important-priority announcements are pinned at the top and
/// bypass notification aggregation for immediate delivery.
/// </summary>
public class Announcement : ITranslatable
{
    public Guid Id { get; set; }

    // ── Relationship Keys ────────────────────────────────────────
    public Guid CourseId { get; set; }
    public Guid CreatedByUserId { get; set; }

    // ── Content ──────────────────────────────────────────────────
    /// <summary>Short title of the announcement (shown in list and notification).</summary>
    [Translatable]
    public string Title { get; set; } = string.Empty;

    /// <summary>Full content of the announcement (supports multi-line text).</summary>
    [Translatable]
    public string Content { get; set; } = string.Empty;

    public string? Translations { get; set; }

    // ── Priority ─────────────────────────────────────────────────
    /// <summary>
    /// Normal = standard order; Important = pinned top + immediate notification.
    /// </summary>
    public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.Normal;

    // ── Manual Ordering ──────────────────────────────────────
    /// <summary>
    /// Controls the admin/tutor-defined display order within each priority group.
    /// Important announcements are sorted by DisplayOrder within the Important group;
    /// Normal announcements are sorted by DisplayOrder within the Normal group.
    /// Lower value = appears higher in the list.
    /// Defaults to 0; new announcements are appended by assigning max+1.
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    // ── Attachment (optional) ────────────────────────────────────
    /// <summary>Server-relative path to an attached file, if any.</summary>
    public string? AttachmentPath { get; set; }

    /// <summary>External URL attachment (e.g., Google Form link), if any.</summary>
    public string? AttachmentUrl { get; set; }

    // ── Timestamps ──────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ────────────────────────────────────
    public Course? Course { get; set; }
    public User? CreatedByUser { get; set; }
}
