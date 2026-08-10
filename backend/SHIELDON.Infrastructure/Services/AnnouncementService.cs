using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Features.Courses.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;

using SHIELDON.Application.Common;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Implements course announcement creation, listing, deletion, and manual reordering.
/// Enforces RBAC: only Admin / assigned Tutor can create/delete/reorder.
/// Any course-accessible user (enrolled student or Admin/Tutor) can list.
/// Important announcements are pinned at the top of the feed;
/// within each priority group, announcements are sorted by DisplayOrder (ascending).
/// </summary>
public class AnnouncementService : IAnnouncementService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IUserActivityLogger _activityLogger;

    public AnnouncementService(AppDbContext db, INotificationService notificationService, IUserActivityLogger? activityLogger = null)
    {
        _db = db;
        _notificationService = notificationService;
        _activityLogger = activityLogger ?? new NullUserActivityLogger();
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

        // Auto-assign DisplayOrder: append to the bottom of this priority group
        var maxOrder = await _db.Announcements
            .Where(a => a.CourseId == courseId && a.Priority == priority)
            .Select(a => (int?)a.DisplayOrder)
            .MaxAsync(ct);

        var newDisplayOrder = (maxOrder ?? -1) + 1;

        var announcement = new Announcement
        {
            CourseId = courseId,
            CreatedByUserId = requestingUserId,
            Title = request.Title.Trim(),
            Content = SHIELDON.Infrastructure.Common.SanitizationHelper.StripHtml(request.Content),
            Priority = priority,
            DisplayOrder = newDisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync(ct);

        await _activityLogger.LogAsync(
            requestingUserId,
            "CONTENT",
            "AnnouncementPosted",
            $"Posted announcement '{announcement.Title}' in course: {course.Title}",
            entityId: announcement.Id.ToString(),
            entityType: "Announcement",
            ct: ct);

        // Notify enrolled students
        var enrolledStudentIds = await _db.CourseEnrollments
            .Where(e => e.CourseId == courseId && e.Status == CourseEnrollmentStatus.Approved)
            .Select(e => e.StudentId)
            .ToListAsync(ct);

        bool isImportant = priority == AnnouncementPriority.Important;
        var notifType = isImportant ? NotificationType.ImportantCourseAnnouncement : NotificationType.NewCourseAnnouncement;

        foreach (var studentId in enrolledStudentIds)
        {
            await _notificationService.TriggerNotificationAsync(
                studentId,
                isImportant ? "Important Course Announcement" : "New Course Announcement",
                $"An announcement '{announcement.Title}' was posted in '{course.Title}'.",
                $"/courses/{course.Id}?tab=announcements",
                notifType,
                course.Id,
                sendEmail: isImportant, // Only send email if Important
                ct);
        }

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
            // Important group first, then ordered by the admin/tutor-defined DisplayOrder
            .OrderByDescending(a => a.Priority == AnnouncementPriority.Important)
            .ThenBy(a => a.DisplayOrder)
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

        announcement.IsDeleted = true;
        announcement.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _activityLogger.LogAsync(
            requestingUserId,
            "CONTENT",
            "AnnouncementDeleted",
            $"Deleted announcement '{announcement.Title}'",
            entityId: announcement.Id.ToString(),
            entityType: "Announcement",
            ct: ct);
    }

    // ── Reorder ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task ReorderAnnouncementsAsync(
        Guid courseId,
        ReorderAnnouncementsRequest request,
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
            throw new ForbiddenException("You can only reorder announcements for courses assigned to you.");

        // Validate that Items is not empty
        if (request.Items == null || request.Items.Count == 0)
            throw new BusinessRuleException("Reorder request must contain at least one item.");

        // Load all existing announcements for this course
        var existingAnnouncements = await _db.Announcements
            .Where(a => a.CourseId == courseId)
            .ToListAsync(ct);

        // Validate: every submitted ID must belong to this course
        var existingIds = existingAnnouncements.Select(a => a.Id).ToHashSet();
        var submittedIds = request.Items.Select(i => i.Id).ToHashSet();

        var invalidIds = submittedIds.Except(existingIds).ToList();
        if (invalidIds.Count > 0)
            throw new BusinessRuleException(
                $"The following announcement IDs do not belong to this course: {string.Join(", ", invalidIds)}.");

        // Apply new DisplayOrder values
        // Build a lookup from ID → entity
        var lookup = existingAnnouncements.ToDictionary(a => a.Id);

        foreach (var item in request.Items)
        {
            if (lookup.TryGetValue(item.Id, out var ann))
            {
                ann.DisplayOrder = item.DisplayOrder;
                ann.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    // ── Mapping Helper ────────────────────────────────────────────────────

    private static AnnouncementResponse MapToResponse(Announcement a, User creator) => new(
        a.Id,
        a.CourseId,
        a.Title,
        a.Content,
        a.Priority.ToString(),
        a.DisplayOrder,
        a.CreatedByUserId,
        $"{creator.FirstName} {creator.LastName}",
        a.CreatedAt,
        a.UpdatedAt
    );
}
