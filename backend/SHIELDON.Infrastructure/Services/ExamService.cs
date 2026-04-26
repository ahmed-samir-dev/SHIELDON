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
/// Implements exam lifecycle management:
///   - CRUD (create, read, update, delete)
///   - Publishing (Draft → Published) with notification dispatch
///   - Access control enforced per role
/// </summary>
public class ExamService : IExamService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public ExamService(AppDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<ExamSummaryResponse> CreateExamAsync(
        Guid courseId,
        CreateExamRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new NotFoundException("Course", courseId);

        AuthorizeForCourse(course, requestingUserId, requestingUserRole);
        ValidateExamRequest(request.Title, request.TimeLimit, request.MaxAttempts, request.PassScore, request.ResultVisibility);

        if (!Enum.TryParse<ResultVisibility>(request.ResultVisibility, ignoreCase: true, out var resultVisibility))
            throw new BusinessRuleException($"Invalid ResultVisibility '{request.ResultVisibility}'. Use: Immediate, Scheduled, ManualRelease.");

        var exam = new Exam
        {
            CourseId = courseId,
            Title = request.Title.Trim(),
            Instructions = request.Instructions?.Trim(),
            TimeLimit = request.TimeLimit,
            MaxAttempts = request.MaxAttempts,
            PassScore = request.PassScore,
            ResultVisibility = resultVisibility,
            ScheduledAt = request.ScheduledAt,
            ScheduledReleaseAt = request.ScheduledReleaseAt,
            Status = ExamStatus.Draft,
            CreatedByUserId = requestingUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Exams.Add(exam);
        await _db.SaveChangesAsync(ct);

        return MapToSummary(exam, course.Title, 0);
    }

    // ── Get List ──────────────────────────────────────────────────────────────

    public async Task<PagedResponse<ExamSummaryResponse>> GetExamsAsync(
        Guid courseId,
        ExamQueryParams query,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new NotFoundException("Course", courseId);

        // Students must be enrolled
        if (requestingUserRole == "Student")
        {
            var isEnrolled = await _db.CourseEnrollments.AnyAsync(
                e => e.CourseId == courseId && e.StudentId == requestingUserId && e.Status == CourseEnrollmentStatus.Approved, ct);
            if (!isEnrolled)
                throw new ForbiddenException("You must be enrolled in this course to view exams.");
        }

        var baseQuery = _db.Exams
            .Where(e => e.CourseId == courseId);

        // Students only see Published exams
        if (requestingUserRole == "Student")
            baseQuery = baseQuery.Where(e => e.Status == ExamStatus.Published);

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(e => e.Title.Contains(query.Search));

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<ExamStatus>(query.Status, ignoreCase: true, out var statusFilter))
            baseQuery = baseQuery.Where(e => e.Status == statusFilter);

        var totalCount = await baseQuery.CountAsync(ct);

        var exams = await baseQuery
            .OrderByDescending(e => e.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new
            {
                Exam = e,
                QuestionCount = e.Questions.Count
            })
            .ToListAsync(ct);

        var items = exams.Select(x => MapToSummary(x.Exam, course.Title, x.QuestionCount)).ToList();

        return new PagedResponse<ExamSummaryResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.Page,
            PageSize = query.PageSize
        };
    }

    // ── Get Detail ────────────────────────────────────────────────────────────

    public async Task<ExamDetailResponse> GetExamByIdAsync(
        Guid examId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var exam = await _db.Exams
            .Include(e => e.Course)
            .Include(e => e.CreatedByUser)
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.Id == examId, ct)
            ?? throw new NotFoundException("Exam", examId);

        // Students can only see Published exams from courses they're enrolled in
        if (requestingUserRole == "Student")
        {
            if (exam.Status != ExamStatus.Published)
                throw new ForbiddenException("This exam is not currently available.");

            var isEnrolled = await _db.CourseEnrollments.AnyAsync(
                e => e.CourseId == exam.CourseId && e.StudentId == requestingUserId && e.Status == CourseEnrollmentStatus.Approved, ct);
            if (!isEnrolled)
                throw new ForbiddenException("You must be enrolled in this course to view this exam.");
        }

        var creatorName = exam.CreatedByUser != null
            ? $"{exam.CreatedByUser.FirstName} {exam.CreatedByUser.LastName}"
            : "Unknown";

        return MapToDetail(exam, exam.Course!.Title, exam.Questions.Count, creatorName);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    public async Task<ExamDetailResponse> UpdateExamAsync(
        Guid examId,
        UpdateExamRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var exam = await _db.Exams
            .Include(e => e.Course)
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.Id == examId, ct)
            ?? throw new NotFoundException("Exam", examId);

        AuthorizeForCourse(exam.Course!, requestingUserId, requestingUserRole);

        if (exam.Status == ExamStatus.Closed)
            throw new BusinessRuleException("Closed exams cannot be edited.");

        if (request.Title != null) exam.Title = request.Title.Trim();
        if (request.Instructions != null) exam.Instructions = request.Instructions.Trim();
        if (request.TimeLimit.HasValue) exam.TimeLimit = request.TimeLimit.Value;
        if (request.MaxAttempts.HasValue) exam.MaxAttempts = request.MaxAttempts.Value;
        if (request.PassScore.HasValue) exam.PassScore = request.PassScore.Value;
        if (request.ScheduledAt.HasValue) exam.ScheduledAt = request.ScheduledAt.Value;
        if (request.ScheduledReleaseAt.HasValue) exam.ScheduledReleaseAt = request.ScheduledReleaseAt.Value;

        if (request.ResultVisibility != null)
        {
            if (!Enum.TryParse<ResultVisibility>(request.ResultVisibility, ignoreCase: true, out var vis))
                throw new BusinessRuleException($"Invalid ResultVisibility '{request.ResultVisibility}'.");
            exam.ResultVisibility = vis;
        }

        exam.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var creatorName = "Unknown";
        var creator = await _db.Users.FindAsync(new object[] { exam.CreatedByUserId }, ct);
        if (creator != null) creatorName = $"{creator.FirstName} {creator.LastName}";

        return MapToDetail(exam, exam.Course!.Title, exam.Questions.Count, creatorName);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    public async Task DeleteExamAsync(
        Guid examId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var exam = await _db.Exams
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == examId, ct)
            ?? throw new NotFoundException("Exam", examId);

        AuthorizeForCourse(exam.Course!, requestingUserId, requestingUserRole);

        if (exam.Status != ExamStatus.Draft)
            throw new BusinessRuleException("Only Draft exams can be deleted. Published/Closed exams cannot be removed.");

        _db.Exams.Remove(exam);
        await _db.SaveChangesAsync(ct);
    }

    // ── Publish ───────────────────────────────────────────────────────────────

    public async Task<ExamDetailResponse> PublishExamAsync(
        Guid examId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var exam = await _db.Exams
            .Include(e => e.Course)
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.Id == examId, ct)
            ?? throw new NotFoundException("Exam", examId);

        AuthorizeForCourse(exam.Course!, requestingUserId, requestingUserRole);

        if (exam.Status != ExamStatus.Draft)
            throw new BusinessRuleException($"Exam is already '{exam.Status}' and cannot be published again.");

        if (!exam.Questions.Any())
            throw new BusinessRuleException("Cannot publish an exam with no questions. Add at least one question first.");

        exam.Status = ExamStatus.Published;
        exam.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // ── Notify all enrolled students ──────────────────────────────────────
        var enrolledStudentIds = await _db.CourseEnrollments
            .Where(e => e.CourseId == exam.CourseId && e.Status == CourseEnrollmentStatus.Approved)
            .Select(e => e.StudentId)
            .ToListAsync(ct);

        string scheduledInfo = exam.ScheduledAt.HasValue
            ? $" It is scheduled for {exam.ScheduledAt.Value:dd MMM yyyy, HH:mm} UTC."
            : string.Empty;

        foreach (var studentId in enrolledStudentIds)
        {
            await _notificationService.TriggerNotificationAsync(
                studentId,
                "New Exam Available",
                $"A new exam '{exam.Title}' has been published in '{exam.Course!.Title}'.{scheduledInfo}",
                $"/courses/{exam.CourseId}?tab=exams",
                NotificationType.ExamScheduled,
                exam.Id,
                sendEmail: true,
                ct);
        }

        var creatorName = "Unknown";
        var creator = await _db.Users.FindAsync(new object[] { exam.CreatedByUserId }, ct);
        if (creator != null) creatorName = $"{creator.FirstName} {creator.LastName}";

        return MapToDetail(exam, exam.Course!.Title, exam.Questions.Count, creatorName);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private static void AuthorizeForCourse(Course course, Guid userId, string role)
    {
        if (role == "Tutor" && course.AssignedTutorId != userId)
            throw new ForbiddenException("You can only manage exams for courses assigned to you.");
    }

    private static void ValidateExamRequest(string title, int timeLimit, int maxAttempts, decimal passScore, string resultVisibility)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new BusinessRuleException("Exam title cannot be empty.");
        if (timeLimit < 1)
            throw new BusinessRuleException("Time limit must be at least 1 minute.");
        if (maxAttempts < 1)
            throw new BusinessRuleException("Max attempts must be at least 1.");
        if (passScore < 0 || passScore > 100)
            throw new BusinessRuleException("Pass score must be between 0 and 100.");
    }

    private static ExamSummaryResponse MapToSummary(Exam e, string courseTitle, int questionCount) => new(
        e.Id,
        e.CourseId,
        courseTitle,
        e.Title,
        e.TimeLimit,
        e.MaxAttempts,
        e.PassScore,
        e.Status.ToString(),
        e.ResultVisibility.ToString(),
        e.ScheduledAt,
        e.ScheduledReleaseAt,
        questionCount,
        e.CreatedAt
    );

    private static ExamDetailResponse MapToDetail(Exam e, string courseTitle, int questionCount, string createdByName) => new(
        e.Id,
        e.CourseId,
        courseTitle,
        e.Title,
        e.Instructions,
        e.TimeLimit,
        e.MaxAttempts,
        e.PassScore,
        e.Status.ToString(),
        e.ResultVisibility.ToString(),
        e.ScheduledAt,
        e.ScheduledReleaseAt,
        questionCount,
        createdByName,
        e.CreatedAt,
        e.UpdatedAt
    );
}
