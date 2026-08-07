namespace SHIELDON.Application.Features.Courses.DTOs;

// ── Announcement Responses ──────────────────────────────────────────────────

/// <summary>
/// Response returned for a single course announcement.
/// Important-priority announcements are shown pinned at the top of the feed.
/// </summary>
public record AnnouncementResponse(
    Guid Id,
    Guid CourseId,
    string Title,
    string Content,
    string Priority,            // "Normal" or "Important"
    int DisplayOrder,           // Manual ordering index within the priority group
    Guid CreatedByUserId,
    string CreatedByName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

