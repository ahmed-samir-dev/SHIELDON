using SHIELDON.Application.Features.Courses.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Announcement management service contract.
/// Handles creation, listing, and deletion of course announcements.
/// Access rules:
/// - Only Admin or the assigned Tutor may create/delete.
/// - Any authenticated user with course access (enrolled student or Admin/Tutor) may list.
/// </summary>
public interface IAnnouncementService
{
    /// <summary>
    /// Posts a new announcement to a course.
    /// Only Admin or the assigned Tutor of the course may post.
    /// Important-priority announcements appear pinned at the top.
    /// </summary>
    Task<AnnouncementResponse> CreateAnnouncementAsync(
        Guid courseId,
        CreateAnnouncementRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all announcements for a course, ordered: Important first, then by CreatedAt descending.
    /// Students must be Approved-enrolled. Admin/Tutor are always allowed.
    /// </summary>
    Task<IReadOnlyList<AnnouncementResponse>> GetAnnouncementsAsync(
        Guid courseId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes an announcement record permanently.
    /// Only Admin or the assigned Tutor of the course may delete.
    /// </summary>
    Task DeleteAnnouncementAsync(
        Guid announcementId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);
}
