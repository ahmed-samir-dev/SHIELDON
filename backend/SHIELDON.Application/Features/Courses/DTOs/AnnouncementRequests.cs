namespace SHIELDON.Application.Features.Courses.DTOs;

// ── Announcement Requests ──────────────────────────────────────────────────

/// <summary>
/// Request to post a new announcement in a course.
/// Priority "Important" pins the announcement at the top of the feed.
/// </summary>
public record CreateAnnouncementRequest(
    string Title,
    string Content,
    string Priority         // "Normal" or "Important"
);
