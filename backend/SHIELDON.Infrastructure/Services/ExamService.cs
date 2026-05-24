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

        if (request.Weight < 0 || request.Weight > 100)
            throw new BusinessRuleException("Weight must be between 0 and 100.");

        var currentWeights = await _db.Assignments.Where(a => a.CourseId == courseId).SumAsync(a => a.Weight, ct) +
                             await _db.Exams.Where(e => e.CourseId == courseId).SumAsync(e => e.Weight, ct);
        
        if (currentWeights + request.Weight > 100m)
            throw new BusinessRuleException($"Total course weight cannot exceed 100%. Current available weight is {100m - currentWeights}%.");

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
            Weight = request.Weight,
            ResultVisibility = resultVisibility,
            ScheduledAt = request.ScheduledAt,
            ScheduledEndAt = request.ScheduledEndAt,
            ScheduledReleaseAt = request.ScheduledReleaseAt,
            Status = ExamStatus.Draft,
            CreatedByUserId = requestingUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Exams.Add(exam);

        // Persist selection rules
        if (request.SelectionRules?.Count > 0)
        {
            foreach (var rule in request.SelectionRules)
            {
                if (!Enum.TryParse<QuestionType>(rule.QuestionType, ignoreCase: true, out var qt))
                    throw new BusinessRuleException($"Invalid QuestionType '{rule.QuestionType}' in selection rule.");
                if (rule.Count < 1)
                    throw new BusinessRuleException($"Selection rule for {rule.QuestionType} must request at least 1 question.");

                _db.ExamSelectionRules.Add(new ExamSelectionRule
                {
                    ExamId = exam.Id,
                    QuestionType = qt,
                    Count = rule.Count
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        var bankCount = await _db.ExamQuestions.CountAsync(q => q.CourseId == courseId, ct);
        return MapToSummary(exam, course.Title, bankCount, []);
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
            .Include(e => e.SelectionRules)
            .ToListAsync(ct);

        var bankCount = await _db.ExamQuestions.CountAsync(q => q.CourseId == courseId, ct);
        
        var latestAttemptIds = new Dictionary<Guid, Guid>();
        if (requestingUserRole == "Student")
        {
            var examIds = exams.Select(e => e.Id).ToList();
            var attempts = await _db.ExamAttempts
                .Where(a => a.StudentId == requestingUserId && examIds.Contains(a.ExamId) && a.Status != AttemptStatus.InProgress)
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync(ct);
            
            foreach (var examId in examIds)
            {
                var latest = attempts.FirstOrDefault(a => a.ExamId == examId);
                if (latest != null)
                {
                    latestAttemptIds[examId] = latest.Id;
                }
            }
        }

        var items = exams.Select(e => MapToSummary(e, course.Title, bankCount, e.SelectionRules.ToList(), latestAttemptIds.GetValueOrDefault(e.Id, Guid.Empty))).ToList();

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
            .Include(e => e.SelectionRules)
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

        var bankCount = await _db.ExamQuestions.CountAsync(q => q.CourseId == exam.CourseId, ct);
        return MapToDetail(exam, exam.Course!.Title, bankCount, exam.SelectionRules.ToList(), creatorName);
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
            .Include(e => e.SelectionRules)
            .FirstOrDefaultAsync(e => e.Id == examId, ct)
            ?? throw new NotFoundException("Exam", examId);

        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == exam.CourseId, ct)
            ?? throw new NotFoundException("Course", exam.CourseId);

        AuthorizeForCourse(course, requestingUserId, requestingUserRole);

        if (exam.Status == ExamStatus.Closed)
            throw new BusinessRuleException("Closed exams cannot be edited.");

        if (request.Title != null && string.IsNullOrWhiteSpace(request.Title))
            throw new BusinessRuleException("Exam title cannot be empty if provided.");

        if (request.Weight.HasValue)
        {
            if (request.Weight.Value < 0 || request.Weight.Value > 100)
                throw new BusinessRuleException("Weight must be between 0 and 100.");

            var currentWeights = await _db.Assignments.Where(a => a.CourseId == course.Id).SumAsync(a => a.Weight, ct) +
                                 await _db.Exams.Where(e => e.CourseId == course.Id && e.Id != examId).SumAsync(e => e.Weight, ct);
            
            if (currentWeights + request.Weight.Value > 100m)
                throw new BusinessRuleException($"Total course weight cannot exceed 100%. Current available weight is {100m - currentWeights}%.");
        }

        bool weightChanged = request.Weight.HasValue && exam.Weight != request.Weight.Value;

        if (request.Title != null) exam.Title = request.Title.Trim();
        if (request.Instructions != null) exam.Instructions = request.Instructions.Trim();
        if (request.TimeLimit.HasValue) exam.TimeLimit = request.TimeLimit.Value;
        if (request.MaxAttempts.HasValue) exam.MaxAttempts = request.MaxAttempts.Value;
        if (request.PassScore.HasValue) exam.PassScore = request.PassScore.Value;
        if (request.Weight.HasValue) exam.Weight = request.Weight.Value;
        if (request.ScheduledAt.HasValue) exam.ScheduledAt = request.ScheduledAt.Value;
        if (request.ScheduledEndAt.HasValue) exam.ScheduledEndAt = request.ScheduledEndAt.Value;
        if (request.ScheduledReleaseAt.HasValue) exam.ScheduledReleaseAt = request.ScheduledReleaseAt.Value;

        if (request.ResultVisibility != null)
        {
            if (!Enum.TryParse<ResultVisibility>(request.ResultVisibility, ignoreCase: true, out var vis))
                throw new BusinessRuleException($"Invalid ResultVisibility '{request.ResultVisibility}'.");
            exam.ResultVisibility = vis;
        }

        // Replace selection rules if provided
        if (request.SelectionRules != null)
        {
            _db.ExamSelectionRules.RemoveRange(exam.SelectionRules);
            foreach (var rule in request.SelectionRules)
            {
                if (!Enum.TryParse<QuestionType>(rule.QuestionType, ignoreCase: true, out var qt))
                    throw new BusinessRuleException($"Invalid QuestionType '{rule.QuestionType}' in selection rule.");
                if (rule.Count < 1)
                    throw new BusinessRuleException($"Selection rule for {rule.QuestionType} must request at least 1 question.");

                _db.ExamSelectionRules.Add(new ExamSelectionRule
                {
                    ExamId = exam.Id,
                    QuestionType = qt,
                    Count = rule.Count
                });
            }
        }

        exam.UpdatedAt = DateTime.UtcNow;

        if (weightChanged)
        {
            var relatedGrades = await _db.GradeRecords.Where(g => g.ExamId == examId).ToListAsync(ct);
            foreach (var grade in relatedGrades)
            {
                grade.Weight = request.Weight!.Value;
                grade.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);

        // Reload selection rules after save
        await _db.Entry(exam).Collection(e => e.SelectionRules).LoadAsync(ct);

        var creatorName = "Unknown";
        var creator = await _db.Users.FindAsync(new object[] { exam.CreatedByUserId }, ct);
        if (creator != null) creatorName = $"{creator.FirstName} {creator.LastName}";

        var bankCount = await _db.ExamQuestions.CountAsync(q => q.CourseId == exam.CourseId, ct);
        return MapToDetail(exam, exam.Course!.Title, bankCount, exam.SelectionRules.ToList(), creatorName);
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

        bool isDraft = exam.Status == ExamStatus.Draft;
        bool isExpired = exam.ScheduledEndAt.HasValue && exam.ScheduledEndAt.Value < DateTime.UtcNow;

        if (!isDraft && !isExpired)
            throw new BusinessRuleException(
                "Only Draft exams or expired Published exams can be permanently deleted.");

        // Manually delete entities that have Restrict foreign keys to Exam to prevent DbUpdateException
        var violationLogs = await _db.ViolationLogs.Where(v => v.ExamId == examId).ToListAsync(ct);
        if (violationLogs.Any()) _db.ViolationLogs.RemoveRange(violationLogs);

        // Remove these safely just in case cascade delete fails on SQL Server side
        var extensions = await _db.ExamExtensions.Where(e => e.ExamId == examId).ToListAsync(ct);
        if (extensions.Any()) _db.ExamExtensions.RemoveRange(extensions);

        var reattemptRequests = await _db.ReattemptRequests.Where(r => r.ExamId == examId).ToListAsync(ct);
        if (reattemptRequests.Any()) _db.ReattemptRequests.RemoveRange(reattemptRequests);

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
            .Include(e => e.SelectionRules)
            .FirstOrDefaultAsync(e => e.Id == examId, ct)
            ?? throw new NotFoundException("Exam", examId);

        AuthorizeForCourse(exam.Course!, requestingUserId, requestingUserRole);

        if (exam.Status != ExamStatus.Draft)
            throw new BusinessRuleException($"Exam is already '{exam.Status}' and cannot be published again.");

        if (!exam.SelectionRules.Any())
            throw new BusinessRuleException("Cannot publish an exam with no question selection rules. Add at least one selection rule first.");

        // Validate bank has enough questions per type
        foreach (var rule in exam.SelectionRules)
        {
            var available = await _db.ExamQuestions
                .CountAsync(q => q.CourseId == exam.CourseId && q.Type == rule.QuestionType, ct);

            if (available < rule.Count)
            {
                var typeName = rule.QuestionType.ToString();
                throw new BusinessRuleException(
                    $"Not enough {typeName} questions in the bank. Need {rule.Count}, found {available}.");
            }
        }

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

        var bankCount = await _db.ExamQuestions.CountAsync(q => q.CourseId == exam.CourseId, ct);
        return MapToDetail(exam, exam.Course!.Title, bankCount, exam.SelectionRules.ToList(), creatorName);
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
        if (string.Equals(resultVisibility, "Scheduled", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException(
                "'Scheduled' result visibility is no longer supported. Use 'Immediate' or 'ManualRelease'.");
    }

    private static ExamSummaryResponse MapToSummary(Exam e, string courseTitle, int bankCount, List<ExamSelectionRule> rules, Guid latestAttemptId = default) => new(
        Id: e.Id,
        CourseId: e.CourseId,
        CourseTitle: courseTitle,
        Title: e.Title,
        Instructions: e.Instructions,
        TimeLimit: e.TimeLimit,
        MaxAttempts: e.MaxAttempts,
        PassScore: e.PassScore,
        Weight: e.Weight,
        Status: e.Status.ToString(),
        ResultVisibility: e.ResultVisibility.ToString(),
        ScheduledAt: e.ScheduledAt,
        ScheduledEndAt: e.ScheduledEndAt,
        ScheduledReleaseAt: e.ScheduledReleaseAt,
        BankQuestionCount: bankCount,
        SelectionRules: rules.Select(r => new ExamSelectionRuleResponse(r.Id, r.QuestionType.ToString(), r.Count)).ToList(),
        CreatedAt: e.CreatedAt,
        LatestAttemptId: latestAttemptId == Guid.Empty ? null : latestAttemptId
    );

    private static ExamDetailResponse MapToDetail(Exam e, string courseTitle, int bankCount, List<ExamSelectionRule> rules, string createdByName) => new(
        Id: e.Id,
        CourseId: e.CourseId,
        CourseTitle: courseTitle,
        Title: e.Title,
        Instructions: e.Instructions,
        TimeLimit: e.TimeLimit,
        MaxAttempts: e.MaxAttempts,
        PassScore: e.PassScore,
        Weight: e.Weight,
        Status: e.Status.ToString(),
        ResultVisibility: e.ResultVisibility.ToString(),
        ScheduledAt: e.ScheduledAt,
        ScheduledEndAt: e.ScheduledEndAt,
        ScheduledReleaseAt: e.ScheduledReleaseAt,
        BankQuestionCount: bankCount,
        SelectionRules: rules.Select(r => new ExamSelectionRuleResponse(r.Id, r.QuestionType.ToString(), r.Count)).ToList(),
        CreatedByName: createdByName,
        CreatedAt: e.CreatedAt,
        UpdatedAt: e.UpdatedAt
    );
}
