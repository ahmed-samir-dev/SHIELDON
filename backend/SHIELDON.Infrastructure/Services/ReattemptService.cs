using Microsoft.AspNetCore.Hosting;
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
/// Implements the request lifecycle for:
///   1. Re-attempt requests: student had a failed/expired attempt and wants another try.
///   2. Re-open requests: student never entered the exam (0 attempts) and the exam has expired.
///      On approval, an ExamExtension row is created granting that student a personal deadline.
/// </summary>
public class ReattemptService : IReattemptService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IWebHostEnvironment _env;

    private static readonly HashSet<string> AllowedAttachmentExtensions =
        [".jpg", ".jpeg", ".png", ".pdf", ".docx"];
    private const long MaxAttachmentSizeBytes = 10 * 1024 * 1024; // 10 MB

    public ReattemptService(AppDbContext db, INotificationService notificationService, IWebHostEnvironment env)
    {
        _db = db;
        _notificationService = notificationService;
        _env = env;
    }

    // ── Submit ────────────────────────────────────────────────────────────────

    public async Task<StudentReattemptStatusResponse> SubmitRequestAsync(
        Guid examId,
        Guid studentId,
        SubmitReattemptRequest request,
        Stream? attachmentStream,
        string? attachmentFileName,
        long attachmentSize,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Justification))
            throw new BusinessRuleException("A justification is required when submitting a request.");

        // Load exam with course
        var exam = await _db.Exams
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == examId, ct)
            ?? throw new NotFoundException("Exam", examId);

        if (exam.Status != ExamStatus.Published)
            throw new BusinessRuleException("You can only submit a request for a published exam.");

        // Verify enrollment
        var isEnrolled = await _db.CourseEnrollments.AnyAsync(
            e => e.CourseId == exam.CourseId
              && e.StudentId == studentId
              && e.Status == CourseEnrollmentStatus.Approved, ct);
        if (!isEnrolled)
            throw new ForbiddenException("You must be enrolled in this course to submit this request.");

        // Count completed (non-InProgress) attempts
        var attemptsMade = await _db.ExamAttempts.CountAsync(
            a => a.ExamId == examId
              && a.StudentId == studentId
              && a.Status != AttemptStatus.InProgress, ct);

        // Block duplicate pending request
        var existing = await _db.ReattemptRequests.FirstOrDefaultAsync(
            r => r.ExamId == examId
              && r.StudentId == studentId
              && r.Status == "Pending", ct);
        if (existing is not null)
            throw new BusinessRuleException("You already have a pending request for this exam.");

        if (request.IsReopenRequest)
        {
            // Re-open validation: student must have 0 attempts and exam must be expired
            if (attemptsMade > 0)
                throw new BusinessRuleException(
                    "Re-open requests are only for students who never entered the exam. " +
                    "If you have already attempted the exam, please use the standard re-attempt request instead.");

            if (exam.ScheduledEndAt == null || exam.ScheduledEndAt > DateTime.UtcNow)
                throw new BusinessRuleException(
                    "The exam has not expired yet. You can only request a re-open after the exam's end time has passed.");
        }
        else
        {
            // Standard re-attempt: student must have exhausted all attempts
            if (attemptsMade < exam.MaxAttempts)
                throw new BusinessRuleException(
                    $"You still have {exam.MaxAttempts - attemptsMade} attempt(s) remaining. " +
                    "Re-attempt requests are only allowed after all attempts are exhausted.");
        }

        // Handle optional attachment file
        string? attachmentUrl = null;
        if (attachmentStream is not null && attachmentSize > 0)
        {
            var ext = Path.GetExtension(attachmentFileName ?? "").ToLowerInvariant();
            if (!AllowedAttachmentExtensions.Contains(ext))
                throw new BusinessRuleException(
                    $"Invalid attachment type '{ext}'. Allowed: {string.Join(", ", AllowedAttachmentExtensions)}");

            if (attachmentSize > MaxAttachmentSizeBytes)
                throw new BusinessRuleException("Attachment is too large. Maximum allowed size is 10 MB.");

            var folder = Path.Combine(_env.WebRootPath, "Uploads", "requests");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(folder, fileName);

            await using var fileStream = new FileStream(filePath, FileMode.Create);
            await attachmentStream.CopyToAsync(fileStream, ct);
            attachmentUrl = $"/Uploads/requests/{fileName}";
        }

        // Create request
        var reattempt = new ReattemptRequest
        {
            StudentId = studentId,
            ExamId = examId,
            Justification = request.Justification.Trim(),
            IsReopenRequest = request.IsReopenRequest,
            AttachmentUrl = attachmentUrl,
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
            request.IsReopenRequest,
            ct);

        return MapToStudentResponse(reattempt, exam.Title, attemptsMade, exam.MaxAttempts, null);
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

            // Load extension if it exists for any approved request
            DateTime? extensionUntil = null;
            if (r.Status == "Approved")
            {
                var extension = await _db.ExamExtensions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.StudentId == r.StudentId && e.ExamId == r.ExamId, ct);
                extensionUntil = extension?.ExtendedEndTime;
            }

            responses.Add(MapToFullResponse(r, attemptsMade, extensionUntil));
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

            DateTime? extensionUntil = null;
            if (r.Status == "Approved")
            {
                var extension = await _db.ExamExtensions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.StudentId == r.StudentId && e.ExamId == r.ExamId, ct);
                extensionUntil = extension?.ExtendedEndTime;
            }

            result.Add(MapToStudentResponse(r, r.Exam!.Title, attemptsMade, r.Exam.MaxAttempts, extensionUntil));
        }
        return result;
    }

    // ── CanStudentSubmitReopenRequest ─────────────────────────────────────────

    public async Task<bool> CanStudentSubmitReopenRequestAsync(Guid examId, Guid studentId, CancellationToken ct = default)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId, ct);
        if (exam is null || exam.Status != ExamStatus.Published) return false;

        // Exam must be expired
        if (exam.ScheduledEndAt == null || exam.ScheduledEndAt > DateTime.UtcNow) return false;

        // Student must have 0 completed attempts
        var attemptsMade = await _db.ExamAttempts.CountAsync(
            a => a.ExamId == examId && a.StudentId == studentId && a.Status != AttemptStatus.InProgress, ct);
        if (attemptsMade > 0) return false;

        // No existing pending re-open request
        var hasPending = await _db.ReattemptRequests.AnyAsync(
            r => r.ExamId == examId && r.StudentId == studentId && r.IsReopenRequest && r.Status == "Pending", ct);

        // No already approved extension
        var hasExtension = await _db.ExamExtensions.AnyAsync(
            e => e.ExamId == examId && e.StudentId == studentId, ct);

        return !hasPending && !hasExtension;
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
            ?? throw new NotFoundException("Request", requestId);

        if (reattempt.Status != "Pending")
            throw new BusinessRuleException($"Only Pending requests can be reviewed. This request is already '{reattempt.Status}'.");

        // Tutor access check
        if (reviewerRole == "Tutor" && reattempt.Exam!.Course!.AssignedTutorId != reviewerId)
            throw new ForbiddenException("You can only review requests for exams in your assigned courses.");

        reattempt.ReviewedById = reviewerId;
        reattempt.ReviewedAt = DateTime.UtcNow;
        reattempt.Status = request.Approved ? "Approved" : "Rejected";
        reattempt.RejectionReason = request.Approved ? null : request.RejectionReason?.Trim();

        DateTime? grantedExtensionUntil = null;

        // For approved requests: create an ExamExtension record if hours are provided
        if (request.Approved && request.ExtensionHours.HasValue && request.ExtensionHours.Value > 0)
        {
            int hours = request.ExtensionHours.Value;
            grantedExtensionUntil = DateTime.UtcNow.AddHours(hours);

            // Upsert: remove old extension if exists (re-review scenario)
            var oldExtension = await _db.ExamExtensions.FirstOrDefaultAsync(
                e => e.StudentId == reattempt.StudentId && e.ExamId == reattempt.ExamId, ct);
            if (oldExtension is not null)
                _db.ExamExtensions.Remove(oldExtension);

            _db.ExamExtensions.Add(new ExamExtension
            {
                StudentId = reattempt.StudentId,
                ExamId = reattempt.ExamId,
                ExtendedEndTime = grantedExtensionUntil.Value,
                SourceRequestId = reattempt.Id,
                GrantedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);

        // Notify the student
        if (request.Approved)
        {
            var extHours = request.ExtensionHours ?? 24;
            var message = reattempt.IsReopenRequest
                ? $"Your re-open request for '{reattempt.Exam!.Title}' has been approved. You have {extHours} hours to complete the exam."
                : $"Your re-attempt request for '{reattempt.Exam!.Title}' has been approved. You have an extension of {extHours} hours to take the exam again.";

            await _notificationService.TriggerNotificationAsync(
                reattempt.StudentId,
                "Request Approved",
                message,
                $"/courses/{reattempt.Exam!.CourseId}?tab=exams",
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
                "Request Rejected",
                $"Your request for '{reattempt.Exam!.Title}' was not approved.{reason}",
                null,
                NotificationType.ReattemptRejected,
                reattempt.ExamId,
                sendEmail: true,
                ct);
        }

        // Reload ReviewedBy for response mapping
        await _db.Entry(reattempt).Reference(r => r.ReviewedBy).LoadAsync(ct);

        var attemptsMade = await _db.ExamAttempts.CountAsync(
            a => a.ExamId == reattempt.ExamId
              && a.StudentId == reattempt.StudentId
              && a.Status != AttemptStatus.InProgress, ct);

        return MapToFullResponse(reattempt, attemptsMade, grantedExtensionUntil);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task NotifyReviewersAsync(
        Guid courseId, Guid? tutorId, string examTitle, Guid studentId, Guid requestId,
        bool isReopenRequest, CancellationToken ct)
    {
        var student = await _db.Users.FindAsync([studentId], ct);
        var studentName = student is not null ? $"{student.FirstName} {student.LastName}" : "A student";
        var requestType = isReopenRequest ? "re-open" : "re-attempt";

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
                $"New {char.ToUpper(requestType[0])}{requestType[1..]} Request",
                $"{studentName} has submitted a {requestType} request for exam '{examTitle}'.",
                "/reattempt-requests",
                NotificationType.ReattemptRequestReceived,
                requestId,
                sendEmail: false,
                ct);
        }
    }

    private static ReattemptRequestResponse MapToFullResponse(ReattemptRequest r, int attemptsMade, DateTime? grantedExtensionUntil)
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
            AttachmentUrl: r.AttachmentUrl,
            IsReopenRequest: r.IsReopenRequest,
            Status: r.Status,
            AttemptsMade: attemptsMade,
            MaxAttempts: r.Exam?.MaxAttempts ?? 0,
            RequestedAt: r.RequestedAt,
            ReviewedAt: r.ReviewedAt,
            ReviewedByName: reviewerName,
            RejectionReason: r.RejectionReason,
            GrantedExtensionUntil: grantedExtensionUntil
        );
    }

    private static StudentReattemptStatusResponse MapToStudentResponse(
        ReattemptRequest r, string examTitle, int attemptsMade, int maxAttempts, DateTime? grantedExtensionUntil)
    {
        return new StudentReattemptStatusResponse(
            Id: r.Id,
            ExamId: r.ExamId,
            ExamTitle: examTitle,
            Justification: r.Justification,
            AttachmentUrl: r.AttachmentUrl,
            IsReopenRequest: r.IsReopenRequest,
            Status: r.Status,
            AttemptsMade: attemptsMade,
            MaxAttempts: maxAttempts,
            RequestedAt: r.RequestedAt,
            ReviewedAt: r.ReviewedAt,
            RejectionReason: r.RejectionReason,
            GrantedExtensionUntil: grantedExtensionUntil
        );
    }
}
