namespace SHIELDON.Application.Features.Courses.DTOs;

// ── Announcement Reorder DTOs ────────────────────────────────────────────────

/// <summary>
/// A single item in a reorder request - maps one announcement to its new position.
/// </summary>
/// <param name="Id">The GUID of the announcement being repositioned.</param>
/// <param name="DisplayOrder">
/// The new display order index (0-based, lower = higher in the list).
/// </param>
public record AnnouncementOrderItem(
    Guid Id,
    int DisplayOrder
);

/// <summary>
/// Request body for PUT /api/courses/{courseId}/announcements/reorder.
/// The caller submits a complete ordered list of all announcements for the course,
/// providing a new DisplayOrder value for each item.
/// </summary>
public record ReorderAnnouncementsRequest(
    List<AnnouncementOrderItem> Items
);
