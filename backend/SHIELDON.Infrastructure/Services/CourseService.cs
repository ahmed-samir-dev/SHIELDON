using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Courses.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Implements all course management and enrollment workflow operations.
/// Business rules from CLAUDE.md §17 (Feature 5) are enforced here.
/// </summary>
public class CourseService : ICourseService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    // Rejection limits per CLAUDE.md spec
    private const int ConsecutiveRejectionsBeforeCooldown = 2;
    private const int MaxTotalRejectionsBeforePermanentBlock = 3;
    private static readonly TimeSpan CooldownDuration = TimeSpan.FromHours(24);

    public CourseService(AppDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    // ── Course CRUD ──────────────────────────────────────────────────────

    public async Task<CourseResponse> CreateCourseAsync(Guid adminId, CreateCourseRequest request, CancellationToken ct = default)
    {
        // Validate CourseCode uniqueness
        var codeExists = await _db.Courses.AnyAsync(c => c.CourseCode == request.CourseCode.Trim().ToUpper(), ct);
        if (codeExists)
            throw new BusinessRuleException($"Course code '{request.CourseCode}' is already in use. Choose a different code.");

        // Validate assigned tutor exists and has Tutor role, if provided
        if (request.AssignedTutorId.HasValue)
        {
            var tutor = await _db.Users.FindAsync([request.AssignedTutorId.Value], ct);
            if (tutor is null || tutor.Role != UserRole.Tutor)
                throw new NotFoundException("Tutor", request.AssignedTutorId.Value);
        }

        var course = new Course
        {
            Title = request.Title.Trim(),
            CourseCode = request.CourseCode.Trim().ToUpper(),
            Description = request.Description?.Trim(),
            AssignedTutorId = request.AssignedTutorId,
            CreatedByAdminId = adminId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync(ct);

        return await BuildCourseResponseAsync(course.Id, ct);
    }

    public async Task<PagedResponse<CourseResponse>> GetCoursesAsync(
        CourseQueryParams query, Guid requestingUserId, string requestingUserRole, CancellationToken ct = default)
    {
        var q = _db.Courses
            .Include(c => c.AssignedTutor)
            .Include(c => c.Enrollments)
            .AsNoTracking()
            .AsQueryable();

        // Tutors only see their assigned courses
        if (requestingUserRole == "Tutor")
            q = q.Where(c => c.AssignedTutorId == requestingUserId);

        // Students only see active courses
        if (requestingUserRole == "Student")
        {
            q = q.Where(c => c.IsActive);

            if (!string.IsNullOrEmpty(query.EnrollmentStatus))
            {
                var filter = query.EnrollmentStatus.Trim().ToLower();
                if (filter == "enrolled")
                    q = q.Where(c => c.Enrollments.Any(e => e.StudentId == requestingUserId && e.Status == CourseEnrollmentStatus.Approved));
                else if (filter == "pending")
                    q = q.Where(c => c.Enrollments.Any(e => e.StudentId == requestingUserId && e.Status == CourseEnrollmentStatus.Pending));
                else if (filter == "unenrolled")
                    q = q.Where(c => !c.Enrollments.Any(e => e.StudentId == requestingUserId && (e.Status == CourseEnrollmentStatus.Approved || e.Status == CourseEnrollmentStatus.Pending)));
            }
        }

        // Admin can filter by IsActive
        if (query.IsActive.HasValue)
            q = q.Where(c => c.IsActive == query.IsActive.Value);

        // Full-text style search on Title and CourseCode
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            q = q.Where(c =>
                c.Title.ToLower().Contains(search) ||
                c.CourseCode.ToLower().Contains(search));
        }

        var totalCount = await q.CountAsync(ct);

        var courses = await q
            .OrderByDescending(c => c.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var items = courses.Select(c => MapToCourseResponse(c)).ToList();

        return new PagedResponse<CourseResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<CourseDetailResponse> GetCourseByIdAsync(Guid courseId, CancellationToken ct = default)
    {
        var course = await _db.Courses
            .Include(c => c.AssignedTutor)
            .Include(c => c.Enrollments)
            .Include(c => c.Materials)
            .Include(c => c.Announcements)
            .Include(c => c.Assignments)
            .Include(c => c.Exams)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new NotFoundException("Course", courseId);

        var tutorName = course.AssignedTutor is not null
            ? $"{course.AssignedTutor.FirstName} {course.AssignedTutor.LastName}"
            : null;

        var approvedEnrollments = course.Enrollments
            .Count(e => e.Status == CourseEnrollmentStatus.Approved);

        var publishedExamCount = course.Exams.Count(e => e.Status == ExamStatus.Published);

        return new CourseDetailResponse(
            course.Id,
            course.Title,
            course.CourseCode,
            course.Description,
            course.AssignedTutorId,
            tutorName,
            course.IsActive,
            approvedEnrollments,
            course.Materials.Count,
            course.Announcements.Count,
            course.Assignments.Count,
            course.Exams.Count,
            publishedExamCount,
            course.CreatedAt
        );
    }

    public async Task<CourseResponse> UpdateCourseAsync(Guid courseId, UpdateCourseRequest request, Guid requestingUserId, string requestingUserRole, CancellationToken ct = default)
    {
        var course = await _db.Courses.FindAsync([courseId], ct)
            ?? throw new NotFoundException("Course", courseId);

        if (requestingUserRole == "Tutor" && course.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only edit courses that are assigned to you.");

        // Validate new tutor exists and has Tutor role, if being changed
        if (request.AssignedTutorId.HasValue && request.AssignedTutorId != course.AssignedTutorId)
        {
            var tutor = await _db.Users.FindAsync([request.AssignedTutorId.Value], ct);
            if (tutor is null || tutor.Role != UserRole.Tutor)
                throw new NotFoundException("Tutor", request.AssignedTutorId.Value);
        }

        course.Title = request.Title.Trim();
        course.Description = request.Description?.Trim();
        course.AssignedTutorId = request.AssignedTutorId;
        course.IsActive = request.IsActive;
        course.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await BuildCourseResponseAsync(course.Id, ct);
    }

    public async Task DeleteCourseAsync(Guid courseId, CancellationToken ct = default)
    {
        var course = await _db.Courses.FindAsync([courseId], ct)
            ?? throw new NotFoundException("Course", courseId);

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync(ct);
    }

    // ── Enrollment Workflow ──────────────────────────────────────────────

    public async Task<StudentEnrollmentStatusResponse> RequestEnrollmentAsync(
        Guid studentId, EnrollmentRequest request, CancellationToken ct = default)
    {
        // Verify course exists and is active
        var course = await _db.Courses.FindAsync([request.CourseId], ct)
            ?? throw new NotFoundException("Course", request.CourseId);

        if (!course.IsActive)
            throw new BusinessRuleException("This course is not currently accepting enrollment requests.");

        // Check for an existing enrollment record for this student+course
        var existing = await _db.CourseEnrollments
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == request.CourseId, ct);

        if (existing is not null)
        {
            switch (existing.Status)
            {
                case CourseEnrollmentStatus.Approved:
                    throw new BusinessRuleException("You are already enrolled in this course.");

                case CourseEnrollmentStatus.Pending:
                    throw new BusinessRuleException("You already have a pending enrollment request for this course.");

                case CourseEnrollmentStatus.Rejected:
                    // Check permanent block
                    if (existing.RejectionCount >= MaxTotalRejectionsBeforePermanentBlock)
                        throw new BusinessRuleException("You have been permanently blocked from enrolling in this course.");

                    // Check cooldown
                    if (existing.CooldownUntil.HasValue && existing.CooldownUntil.Value > DateTime.UtcNow)
                    {
                        var remaining = existing.CooldownUntil.Value - DateTime.UtcNow;
                        throw new BusinessRuleException(
                            $"You can submit a new request in {(int)remaining.TotalHours} hours and {remaining.Minutes} minutes.");
                    }

                    // Reset to pending for a new attempt
                    existing.Status = CourseEnrollmentStatus.Pending;
                    existing.RejectionReason = null;
                    existing.ReviewedById = null;
                    existing.ReviewedAt = null;
                    existing.RequestedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;
                    // DO NOT reset RejectionCount — it accumulates for the block check

                    await _db.SaveChangesAsync(ct);
                    await NotifyAdminsAndTutorOfRequestAsync(course.Id, course.Title, studentId, ct);
                    return MapToStudentStatus(existing, course.Title);

                case CourseEnrollmentStatus.Dropped:
                    // Treat a dropped enrollment like a fresh request
                    existing.Status = CourseEnrollmentStatus.Pending;
                    existing.RejectionCount = 0;
                    existing.CooldownUntil = null;
                    existing.RejectionReason = null;
                    existing.ReviewedById = null;
                    existing.ReviewedAt = null;
                    existing.RequestedAt = DateTime.UtcNow;
                    existing.UpdatedAt = DateTime.UtcNow;

                    await _db.SaveChangesAsync(ct);
                    await NotifyAdminsAndTutorOfRequestAsync(course.Id, course.Title, studentId, ct);
                    return MapToStudentStatus(existing, course.Title);
            }
        }

        // No prior record — create a fresh enrollment request
        var enrollment = new CourseEnrollment
        {
            StudentId = studentId,
            CourseId = request.CourseId,
            Status = CourseEnrollmentStatus.Pending,
            RejectionCount = 0,
            RequestedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.CourseEnrollments.Add(enrollment);
        await _db.SaveChangesAsync(ct);

        await NotifyAdminsAndTutorOfRequestAsync(course.Id, course.Title, studentId, ct);

        return MapToStudentStatus(enrollment, course.Title);
    }

    private async Task NotifyAdminsAndTutorOfRequestAsync(Guid courseId, string courseTitle, Guid studentId, CancellationToken ct)
    {
        var student = await _db.Users.FindAsync(new object[] { studentId }, ct);
        var studentName = student != null ? $"{student.FirstName} {student.LastName}" : "A student";

        var course = await _db.Courses.FindAsync(new object[] { courseId }, ct);
        var tutorId = course?.AssignedTutorId;

        var adminIds = await _db.Users
            .Where(u => u.Role == UserRole.Admin)
            .Select(u => u.Id)
            .ToListAsync(ct);

        var recipientIds = new HashSet<Guid>(adminIds);
        if (tutorId.HasValue)
        {
            recipientIds.Add(tutorId.Value);
        }

        foreach (var recipientId in recipientIds)
        {
            await _notificationService.TriggerNotificationAsync(
                recipientId,
                "New Enrollment Request",
                $"{studentName} has requested to enroll in '{courseTitle}'.",
                "/enrollments",
                NotificationType.CourseUpdate,
                courseId,
                sendEmail: false,
                ct);
        }
    }

    public async Task<IReadOnlyList<EnrollmentResponse>> GetPendingEnrollmentsAsync(
        Guid reviewerId, string reviewerRole, Guid? courseId, CancellationToken ct = default)
    {
        var q = _db.CourseEnrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .Include(e => e.ReviewedBy)
            .AsNoTracking()
            .Where(e => e.Status == CourseEnrollmentStatus.Pending);

        // Tutor can only see enrollments for courses they are assigned to
        if (reviewerRole == "Tutor")
            q = q.Where(e => e.Course!.AssignedTutorId == reviewerId);

        if (courseId.HasValue)
            q = q.Where(e => e.CourseId == courseId.Value);

        var enrollments = await q.OrderBy(e => e.RequestedAt).ToListAsync(ct);

        return enrollments.Select(e => MapToEnrollmentResponse(e)).ToList();
    }

    public async Task<PagedResponse<EnrollmentResponse>> GetApprovedEnrollmentsAsync(
        Guid reviewerId, string reviewerRole, EnrollmentQueryParams query, CancellationToken ct = default)
    {
        var q = _db.CourseEnrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .Include(e => e.ReviewedBy)
            .AsNoTracking()
            .Where(e => e.Status == CourseEnrollmentStatus.Approved);

        // Tutor can only see enrollments for courses they are assigned to
        if (reviewerRole == "Tutor")
            q = q.Where(e => e.Course!.AssignedTutorId == reviewerId);

        if (query.CourseId.HasValue)
            q = q.Where(e => e.CourseId == query.CourseId.Value);

        // ── 1. Filtering ───────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            q = q.Where(e => 
                (e.Student!.FirstName + " " + e.Student!.LastName).ToLower().Contains(search) ||
                e.Student!.Email.ToLower().Contains(search) ||
                e.Student!.StudentId!.ToLower().Contains(search) ||
                e.Course!.Title.ToLower().Contains(search) ||
                e.Course!.CourseCode.ToLower().Contains(search) ||
                (e.ReviewedBy != null && (e.ReviewedBy.FirstName + " " + e.ReviewedBy.LastName).ToLower().Contains(search)) ||
                (e.ReviewedAt.HasValue && (
                    e.ReviewedAt.Value.Year.ToString().Contains(search) ||
                    e.ReviewedAt.Value.Month.ToString().Contains(search) ||
                    e.ReviewedAt.Value.Day.ToString().Contains(search)
                ))
            );
        }

        // ── 2. Pagination ──────────────────────────────────────────────────────
        var totalCount = await q.CountAsync(ct);
        var enrollments = await q
            .OrderByDescending(e => e.ReviewedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResponse<EnrollmentResponse>
        {
            Items = enrollments.Select(e => MapToEnrollmentResponse(e)).ToList(),
            TotalCount = totalCount,
            PageNumber = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<EnrollmentResponse> ReviewEnrollmentAsync(
        Guid enrollmentId, Guid reviewerId, ReviewEnrollmentRequest request, CancellationToken ct = default)
    {
        var enrollment = await _db.CourseEnrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .Include(e => e.ReviewedBy)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, ct)
            ?? throw new NotFoundException("Enrollment", enrollmentId);

        if (enrollment.Status != CourseEnrollmentStatus.Pending)
            throw new BusinessRuleException("Only pending enrollment requests can be reviewed.");

        enrollment.ReviewedById = reviewerId;
        enrollment.ReviewedAt = DateTime.UtcNow;
        enrollment.UpdatedAt = DateTime.UtcNow;

        if (request.Approved)
        {
            enrollment.Status = CourseEnrollmentStatus.Approved;
            enrollment.RejectionReason = null;
            
            if (enrollment.Course!.CourseFee > 0)
            {
                _db.PaymentRecords.Add(new PaymentRecord
                {
                    StudentId = enrollment.StudentId,
                    CourseId = enrollment.CourseId,
                    EnrollmentId = enrollment.Id,
                    AmountUSD = enrollment.Course.CourseFee,
                    Status = PaymentRecordStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
            }
            
            // Notify student: EnrollmentApproved
            await _notificationService.TriggerNotificationAsync(
                enrollment.StudentId,
                "Enrollment Approved",
                $"Your enrollment for '{enrollment.Course!.Title}' has been approved.",
                $"/courses/{enrollment.CourseId}", // Direct link to course
                NotificationType.EnrollmentApproved,
                enrollment.CourseId,
                sendEmail: true,
                ct);
        }
        else
        {
            enrollment.Status = CourseEnrollmentStatus.Rejected;
            enrollment.RejectionReason = request.RejectionReason?.Trim();
            enrollment.RejectionCount++;

            // Apply 24h cooldown after reaching ConsecutiveRejectionsBeforeCooldown
            if (enrollment.RejectionCount % ConsecutiveRejectionsBeforeCooldown == 0)
                enrollment.CooldownUntil = DateTime.UtcNow.Add(CooldownDuration);

            // Notify student: EnrollmentRejected
            await _notificationService.TriggerNotificationAsync(
                enrollment.StudentId,
                "Enrollment Rejected",
                $"Your enrollment for '{enrollment.Course!.Title}' was rejected. " + (string.IsNullOrEmpty(request.RejectionReason) ? "" : $"Reason: {request.RejectionReason}"),
                null, // No valid link
                NotificationType.EnrollmentRejected,
                enrollment.CourseId,
                sendEmail: true,
                ct);
        }

        await _db.SaveChangesAsync(ct);
        return MapToEnrollmentResponse(enrollment);
    }

    public async Task<int> BulkReviewEnrollmentsAsync(
        BulkReviewEnrollmentRequest request, Guid reviewerId, CancellationToken ct = default)
    {
        var enrollments = await _db.CourseEnrollments
            .Include(e => e.Course)
            .Include(e => e.Student)
            .Where(e => request.EnrollmentIds.Contains(e.Id) && e.Status == CourseEnrollmentStatus.Pending)
            .ToListAsync(ct);

        foreach (var enrollment in enrollments)
        {
            enrollment.ReviewedById = reviewerId;
            enrollment.ReviewedAt = DateTime.UtcNow;
            enrollment.UpdatedAt = DateTime.UtcNow;

            if (request.Approved)
            {
                enrollment.Status = CourseEnrollmentStatus.Approved;

                if (enrollment.Course!.CourseFee > 0)
                {
                    _db.PaymentRecords.Add(new PaymentRecord
                    {
                        StudentId = enrollment.StudentId,
                        CourseId = enrollment.CourseId,
                        EnrollmentId = enrollment.Id,
                        AmountUSD = enrollment.Course.CourseFee,
                        Status = PaymentRecordStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            else
            {
                enrollment.Status = CourseEnrollmentStatus.Rejected;
                enrollment.RejectionReason = request.RejectionReason?.Trim();
                enrollment.RejectionCount++;

                if (enrollment.RejectionCount % ConsecutiveRejectionsBeforeCooldown == 0)
                    enrollment.CooldownUntil = DateTime.UtcNow.Add(CooldownDuration);
            }
        }

        
        // Notify the students in bulk
        foreach (var enrollment in enrollments)
        {
            if (request.Approved)
            {
                await _notificationService.TriggerNotificationAsync(
                    enrollment.StudentId,
                    "Enrollment Approved",
                    $"Your enrollment for '{enrollment.Course!.Title}' has been approved.",
                    $"/courses/{enrollment.CourseId}",
                    NotificationType.EnrollmentApproved,
                    enrollment.CourseId,
                    sendEmail: true,
                    ct);
            }
            else
            {
                await _notificationService.TriggerNotificationAsync(
                    enrollment.StudentId,
                    "Enrollment Rejected",
                    $"Your enrollment for '{enrollment.Course!.Title}' was rejected.",
                    null,
                    NotificationType.EnrollmentRejected,
                    enrollment.CourseId,
                    sendEmail: true,
                    ct);
            }
        }

        await _db.SaveChangesAsync(ct);
        return enrollments.Count;
    }

    public async Task<IReadOnlyList<StudentEnrollmentStatusResponse>> GetMyEnrollmentsAsync(
        Guid studentId, CancellationToken ct = default)
    {
        var enrollments = await _db.CourseEnrollments
            .Include(e => e.Course)
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.RequestedAt)
            .ToListAsync(ct);

        return enrollments.Select(e => MapToStudentStatus(e, e.Course!.Title)).ToList();
    }

    // ── Private Helpers ──────────────────────────────────────────────────

    private async Task<CourseResponse> BuildCourseResponseAsync(Guid courseId, CancellationToken ct)
    {
        var course = await _db.Courses
            .Include(c => c.AssignedTutor)
            .Include(c => c.Enrollments)
            .AsNoTracking()
            .FirstAsync(c => c.Id == courseId, ct);

        return MapToCourseResponse(course);
    }

    private static CourseResponse MapToCourseResponse(Course c)
    {
        var tutorName = c.AssignedTutor is not null
            ? $"{c.AssignedTutor.FirstName} {c.AssignedTutor.LastName}"
            : null;

        var approved = c.Enrollments.Count(e => e.Status == CourseEnrollmentStatus.Approved);

        return new CourseResponse(
            c.Id,
            c.Title,
            c.CourseCode,
            c.Description,
            c.AssignedTutorId,
            tutorName,
            c.IsActive,
            approved,
            c.CreatedAt
        );
    }

    private static EnrollmentResponse MapToEnrollmentResponse(CourseEnrollment e)
    {
        var studentDisplayId = e.Student?.StudentId;
        var reviewerName = e.ReviewedBy is not null
            ? $"{e.ReviewedBy.FirstName} {e.ReviewedBy.LastName}"
            : null;

        return new EnrollmentResponse(
            e.Id,
            e.CourseId,
            e.Course?.Title ?? string.Empty,
            e.Course?.CourseCode ?? string.Empty,
            e.StudentId,
            e.Student is not null ? $"{e.Student.FirstName} {e.Student.LastName}" : string.Empty,
            e.Student?.Email ?? string.Empty,
            studentDisplayId,
            e.Status.ToString(),
            e.RejectionCount,
            e.CooldownUntil,
            e.RejectionReason,
            e.RequestedAt,
            e.ReviewedAt,
            reviewerName
        );
    }

    private static StudentEnrollmentStatusResponse MapToStudentStatus(CourseEnrollment e, string courseTitle) =>
        new(
            e.CourseId,
            courseTitle,
            e.Status.ToString(),
            e.RejectionCount,
            e.CooldownUntil,
            e.RejectionReason,
            e.RequestedAt
        );
}
