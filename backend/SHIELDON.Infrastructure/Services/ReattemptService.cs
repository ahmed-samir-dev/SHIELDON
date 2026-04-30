using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Exams.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Implements the re-attempt request lifecycle:
///   - Students request extra attempts after exhausting MaxAttempts
///   - Admin/Tutor reviews and approves/rejects
///   - Notifications sent on every status transition
/// </summary>
public class ReattemptService : IReattemptService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public ReattemptService(AppDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    // ── Submit ────────────────────────────────────────────────────────────────

    public async Task<StudentReattemptStatusResponse> SubmitRequestAsync(
        Guid examId, Guid studentId, SubmitReattemptRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Justification))
            throw new BusinessRuleException("A justification is required when submitting a re-attempt request.");

        // Load exam with course
        var exam = await _db.Exams
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == examId, ct)
            ?? throw new NotFoundException("Exam", examId);

        if (exam.Status != ExamStatus.Published)
            throw new BusinessRuleException("You can only request a re-attempt for a published exam.");

        // Verify enrollment
        var isEnrolled = await _db.CourseEnrollments.AnyAsync(
            e => e.CourseId == exam.CourseId
              && e.StudentId == studentId
              && e.Status == CourseEnrollmentStatus.Approved, ct);
        if (!isEnrolled)
            throw new ForbiddenException("You must be enrolled in this course to request a re-attempt.");

        // Count completed (non-InProgress) attempts
        var attemptsMade = await _db.ExamAttempts.CountAsync(
            a => a.ExamId == examId
              && a.StudentId == studentId
              && a.Status != AttemptStatus.InProgress, ct);

        if (attemptsMade < exam.MaxAttempts)
            throw new BusinessRuleException(
                $"You still have {exam.MaxAttempts - attemptsMade} attempt(s) remaining. " +
                "Re-attempt requests are only allowed after all attempts are exhausted.");

        // Block duplicate pending request
        var existing = await _db.ReattemptRequests.FirstOrDefaultAsync(
            r => r.ExamId == examId
              && r.StudentId == studentId
              && r.Status == "Pending", ct);
        if (existing is not null)
            throw new BusinessRuleException("You already have a pending re-attempt request for this exam.");

        // Create request
        var reattempt = new ReattemptRequest
        {
            StudentId = studentId,
            ExamId = examId,
            Justification = request.Justification.Trim(),
            Status = "Pending",
            RequestedAt = DateTime.UtcNow
        };

        _db.ReattemptRequests.Add(reattempt);
        await _db.SaveChangesAsync(ct);

        // Notify Admin(s) and the course Tutor
        await NotifyReviewersAsync(
            exam.CourseId,
            exam.Course!.AssignedTutorId,
            exam.Title,
            studentId,
            reattempt.Id,
            ct);

        return MapToStudentResponse(reattempt, exam.Title, attemptsMade, exam.MaxAttempts);
    }

    // ── Get Requests (Admin/Tutor view) ───────────────────────────────────────

    public async Task<PagedResponse<ReattemptRequestResponse>> GetRequestsAsync(
        ReattemptQueryParams query, Guid requestingUserId, string requestingUserRole, CancellationToken ct = default)
    {
        var q = _db.ReattemptRequests
            .Include(r => r.Student)
            .Include(r => r.Exam)
                .ThenInclude(e => e!.Course)
            .Include(r => r.ReviewedBy)
            .AsNoTracking()
            .AsQueryable();

        // Tutor: only see requests for their courses
        if (requestingUserRole == "Tutor")
            q = q.Where(r => r.Exam!.Course!.AssignedTutorId == requestingUserId);

        // Student: only their own
        if (requestingUserRole == "Student")
            q = q.Where(r => r.StudentId == requestingUserId);

        if (!string.IsNullOrWhiteSpace(query.Status))
            q = q.Where(r => r.Status == query.Status);

        if (query.ExamId.HasValue)
            q = q.Where(r => r.ExamId == query.ExamId.Value);

        if (query.CourseId.HasValue)
            q = q.Where(r => r.Exam!.CourseId == query.CourseId.Value);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            q = q.Where(r =>
                (r.Student!.FirstName + " " + r.Student.LastName).ToLower().Contains(term) ||
                (r.Student.StudentId != null && r.Student.StudentId.ToLower().Contains(term)) ||
                (r.Exam!.Title.ToLower().Contains(term)) ||
                (r.Exam!.Course!.Title.ToLower().Contains(term))
            );
        }

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(r => r.RequestedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var responses = new List<ReattemptRequestResponse>();
        foreach (var r in items)
        {
            var attemptsMade = await _db.ExamAttempts.CountAsync(
                a => a.ExamId == r.ExamId
                  && a.StudentId == r.StudentId
                  && a.Status != AttemptStatus.InProgress, ct);

            responses.Add(MapToFullResponse(r, attemptsMade));
        }

        return new PagedResponse<ReattemptRequestResponse>
        {
            Items = responses,
            TotalCount = totalCount,
            PageNumber = query.Page,
            PageSize = query.PageSize
        };
    }

    // ── Get My Requests (Student view) ────────────────────────────────────────

    public async Task<IReadOnlyList<StudentReattemptStatusResponse>> GetMyRequestsAsync(
        Guid studentId, CancellationToken ct = default)
    {
        var requests = await _db.ReattemptRequests
            .Include(r => r.Exam)
            .AsNoTracking()
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);

        var result = new List<StudentReattemptStatusResponse>();
        foreach (var r in requests)
        {
            var attemptsMade = await _db.ExamAttempts.CountAsync(
                a => a.ExamId == r.ExamId
                  && a.StudentId == r.StudentId
                  && a.Status != AttemptStatus.InProgress, ct);

            result.Add(MapToStudentResponse(r, r.Exam!.Title, attemptsMade, r.Exam.MaxAttempts));
        }
        return result;
    }

    // ── Review (Admin/Tutor) ──────────────────────────────────────────────────

    public async Task<ReattemptRequestResponse> ReviewRequestAsync(
        Guid requestId, Guid reviewerId, string reviewerRole, ReviewReattemptRequest request, CancellationToken ct = default)
    {
        var reattempt = await _db.ReattemptRequests
            .Include(r => r.Student)
            .Include(r => r.Exam)
                .ThenInclude(e => e!.Course)
            .Include(r => r.ReviewedBy)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new NotFoundException("Re-attempt request", requestId);

        if (reattempt.Status != "Pending")
            throw new BusinessRuleException($"Only Pending requests can be reviewed. This request is already '{reattempt.Status}'.");

        // Tutor access check
        if (reviewerRole == "Tutor" && reattempt.Exam!.Course!.AssignedTutorId != reviewerId)
            throw new ForbiddenException("You can only review re-attempt requests for exams in your assigned courses.");

        reattempt.ReviewedById = reviewerId;
        reattempt.ReviewedAt = DateTime.UtcNow;
        reattempt.Status = request.Approved ? "Approved" : "Rejected";
        reattempt.RejectionReason = request.Approved ? null : request.RejectionReason?.Trim();

        await _db.SaveChangesAsync(ct);

        // Count attempts for response
        var attemptsMade = await _db.ExamAttempts.CountAsync(
            a => a.ExamId == reattempt.ExamId
              && a.StudentId == reattempt.StudentId
              && a.Status != AttemptStatus.InProgress, ct);

        // Notify the student
        if (request.Approved)
        {
            await _notificationService.TriggerNotificationAsync(
                reattempt.StudentId,
                "Re-Attempt Request Approved",
                $"Your re-attempt request for '{reattempt.Exam!.Title}' has been approved. You may now take the exam again.",
                $"/courses/{reattempt.Exam.CourseId}?tab=exams",
                NotificationType.ReattemptApproved,
                reattempt.ExamId,
                sendEmail: true,
                ct);
        }
        else
        {
            var reason = string.IsNullOrWhiteSpace(request.RejectionReason)
                ? string.Empty
                : $" Reason: {request.RejectionReason}";

            await _notificationService.TriggerNotificationAsync(
                reattempt.StudentId,
                "Re-Attempt Request Rejected",
                $"Your re-attempt request for '{reattempt.Exam!.Title}' was not approved.{reason}",
                null,
                NotificationType.ReattemptRejected,
                reattempt.ExamId,
                sendEmail: true,
                ct);
        }

        // Reload ReviewedBy for response mapping
        await _db.Entry(reattempt).Reference(r => r.ReviewedBy).LoadAsync(ct);

        return MapToFullResponse(reattempt, attemptsMade);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task NotifyReviewersAsync(
        Guid courseId, Guid? tutorId, string examTitle, Guid studentId, Guid requestId, CancellationToken ct)
    {
        var student = await _db.Users.FindAsync([studentId], ct);
        var studentName = student is not null ? $"{student.FirstName} {student.LastName}" : "A student";

        var adminIds = await _db.Users
            .Where(u => u.Role == UserRole.Admin)
            .Select(u => u.Id)
            .ToListAsync(ct);

        var recipients = new HashSet<Guid>(adminIds);
        if (tutorId.HasValue)
            recipients.Add(tutorId.Value);

        foreach (var recipientId in recipients)
        {
            await _notificationService.TriggerNotificationAsync(
                recipientId,
                "New Re-Attempt Request",
                $"{studentName} has requested a re-attempt for exam '{examTitle}'.",
                $"/reattempt-requests",
                NotificationType.ReattemptRequestReceived,
                requestId,
                sendEmail: false,
                ct);
        }
    }

    private static ReattemptRequestResponse MapToFullResponse(ReattemptRequest r, int attemptsMade)
    {
        var reviewerName = r.ReviewedBy is not null
            ? $"{r.ReviewedBy.FirstName} {r.ReviewedBy.LastName}"
            : null;

        return new ReattemptRequestResponse(
            Id: r.Id,
            ExamId: r.ExamId,
            ExamTitle: r.Exam?.Title ?? string.Empty,
            CourseId: r.Exam?.CourseId ?? Guid.Empty,
            CourseTitle: r.Exam?.Course?.Title ?? string.Empty,
            StudentId: r.StudentId,
            StudentName: r.Student is not null ? $"{r.Student.FirstName} {r.Student.LastName}" : string.Empty,
            StudentEmail: r.Student?.Email ?? string.Empty,
            StudentDisplayId: r.Student?.StudentId,
            Justification: r.Justification,
            Status: r.Status,
            AttemptsMade: attemptsMade,
            MaxAttempts: r.Exam?.MaxAttempts ?? 0,
            RequestedAt: r.RequestedAt,
            ReviewedAt: r.ReviewedAt,
            ReviewedByName: reviewerName,
            RejectionReason: r.RejectionReason
        );
    }

    private static StudentReattemptStatusResponse MapToStudentResponse(
        ReattemptRequest r, string examTitle, int attemptsMade, int maxAttempts)
    {
        return new StudentReattemptStatusResponse(
            Id: r.Id,
            ExamId: r.ExamId,
            ExamTitle: examTitle,
            Justification: r.Justification,
            Status: r.Status,
            AttemptsMade: attemptsMade,
            MaxAttempts: maxAttempts,
            RequestedAt: r.RequestedAt,
            ReviewedAt: r.ReviewedAt,
            RejectionReason: r.RejectionReason
        );
    }
}
