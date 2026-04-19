using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Features.Courses.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Implements course announcement creation, listing, and deletion.
/// Enforces RBAC: only Admin / assigned Tutor can create/delete.
/// Any course-accessible user (enrolled student or Admin/Tutor) can list.
/// Important announcements are pinned at the top of the feed.
/// </summary>
public class AnnouncementService : IAnnouncementService
{
    private readonly AppDbContext _db;

    public AnnouncementService(AppDbContext db)
    {
        _db = db;
    }

    // ── Create ────────────────────────────────────────────────────────────

    public async Task<AnnouncementResponse> CreateAnnouncementAsync(
        Guid courseId,
        CreateAnnouncementRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        // Verify course exists
        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new NotFoundException("Course", courseId);

        // Authorization: Admin always allowed; Tutor only if assigned to this course
        if (requestingUserRole == "Tutor" && course.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only post announcements to courses assigned to you.");

        // Parse priority
        if (!Enum.TryParse<AnnouncementPriority>(request.Priority, ignoreCase: true, out var priority))
            throw new BusinessRuleException($"Invalid priority '{request.Priority}'. Use 'Normal' or 'Important'.");

        // Validate content
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BusinessRuleException("Announcement title cannot be empty.");

        if (string.IsNullOrWhiteSpace(request.Content))
            throw new BusinessRuleException("Announcement content cannot be empty.");

        // Fetch creator name
        var creator = await _db.Users.FindAsync(new object[] { requestingUserId }, ct)
            ?? throw new NotFoundException("User", requestingUserId);

        var announcement = new Announcement
        {
            CourseId = courseId,
            CreatedByUserId = requestingUserId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            Priority = priority,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync(ct);

        return MapToResponse(announcement, creator);
    }

    // ── List ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<AnnouncementResponse>> GetAnnouncementsAsync(
        Guid courseId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        // Verify course exists
        var course = await _db.Courses
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new NotFoundException("Course", courseId);

        // Students must be Approved-enrolled
        if (requestingUserRole == "Student")
        {
            var isEnrolled = course.Enrollments.Any(e =>
                e.StudentId == requestingUserId &&
                e.Status == CourseEnrollmentStatus.Approved);

            if (!isEnrolled)
                throw new ForbiddenException("You must be enrolled in this course to view announcements.");
        }

        var announcements = await _db.Announcements
            .Include(a => a.CreatedByUser)
            .Where(a => a.CourseId == courseId)
            // Important first, then by date descending (newest on top)
            .OrderByDescending(a => a.Priority == AnnouncementPriority.Important)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        return announcements.Select(a => MapToResponse(a, a.CreatedByUser!)).ToList();
    }

    // ── Delete ────────────────────────────────────────────────────────────

    public async Task DeleteAnnouncementAsync(
        Guid announcementId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var announcement = await _db.Announcements
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == announcementId, ct)
            ?? throw new NotFoundException("Announcement", announcementId);

        // Authorization: Admin always allowed; Tutor only if assigned to this course
        if (requestingUserRole == "Tutor" && announcement.Course!.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only delete announcements from courses assigned to you.");

        _db.Announcements.Remove(announcement);
        await _db.SaveChangesAsync(ct);
    }

    // ── Mapping Helper ────────────────────────────────────────────────────

    private static AnnouncementResponse MapToResponse(Announcement a, User creator) => new(
        a.Id,
        a.CourseId,
        a.Title,
        a.Content,
        a.Priority.ToString(),
        a.CreatedByUserId,
        $"{creator.FirstName} {creator.LastName}",
        a.CreatedAt,
        a.UpdatedAt
    );
}
