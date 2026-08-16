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
/// Handles exam result retrieval, short-answer manual grading, and result publication.
/// </summary>
public class ExamResultService : IExamResultService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    public ExamResultService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    // ── Get Attempt Result (Student / Tutor / Admin) ──────────────────────────

    /// <inheritdoc/>
    public async Task<ApiResponse<ExamResultResponse>> GetAttemptResultAsync(
        Guid attemptId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var attempt = await _db.ExamAttempts
            .AsNoTracking()
            .Include(a => a.Exam)
                .ThenInclude(e => e!.Course)
            .Include(a => a.AttemptQuestions)
                .ThenInclude(aq => aq.Question)
                    .ThenInclude(q => q!.Options)
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct)
            ?? throw new NotFoundException("Exam Attempt", attemptId);

        var exam = attempt.Exam!;

        // Access control
        if (requestingUserRole == "Student")
        {
            if (attempt.StudentId != requestingUserId)
                throw new ForbiddenException("You can only view your own results.");
        }
        else if (requestingUserRole == "Tutor")
        {
            // Tutor must be assigned to the course
            var course = await _db.Courses.FindAsync(new object[] { exam.CourseId }, ct)
                ?? throw new NotFoundException("Course", exam.CourseId);
            if (course.AssignedTutorId != requestingUserId)
                throw new ForbiddenException("You can only view results for exams in your courses.");
        }
        // Admin: unrestricted access

        // Fetch grade record FIRST (needed to determine ManualRelease visibility)
        var gradeRecord = await _db.GradeRecords
            .FirstOrDefaultAsync(g => g.ExamId == exam.Id && g.StudentId == attempt.StudentId, ct);

        bool isPublished = gradeRecord?.IsPublished ?? false;

        // Determine if the result is visible to this student
        bool resultVisible = DetermineResultVisibility(attempt, exam, requestingUserRole, requestingUserId, gradeRecord);

        // Check re-attempt eligibility (only relevant for students)
        bool canRequestReattempt = false;
        if (requestingUserRole == "Student" && attempt.StudentId == requestingUserId)
        {
            canRequestReattempt = await CanRequestReattemptAsync(attempt, exam, ct);
        }

        // Build question reviews - only if result is visible
        IReadOnlyList<QuestionReviewDto>? reviews = null;
        if (resultVisible)
        {
            reviews = BuildQuestionReviews(attempt, exam);
        }

        var response = new ExamResultResponse(
            AttemptId: attempt.Id,
            ExamId: exam.Id,
            CourseId: exam.CourseId,
            ExamTitle: exam.Title,
            CourseTitle: exam.Course?.Title ?? string.Empty,
            Status: attempt.Status,
            StartedAt: attempt.StartedAt,
            SubmittedAt: attempt.SubmittedAt,
            Score: attempt.Score,
            PassScore: exam.PassScore,
            Passed: attempt.Score.HasValue ? attempt.Score >= exam.PassScore : null,
            IsPublished: isPublished,
            ResultVisible: resultVisible,
            QuestionReviews: reviews,
            CanRequestReattempt: canRequestReattempt
        );

        return ApiResponse<ExamResultResponse>.Ok(response);
    }

    // ── Get Student's Attempts ────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<ApiResponse<IReadOnlyList<ExamAttemptSummaryDto>>> GetStudentAttemptsAsync(
        Guid examId,
        Guid requestingUserId,
        CancellationToken ct = default)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId, ct)
            ?? throw new NotFoundException("Exam", examId);

        var attempts = await _db.ExamAttempts
            .Where(a => a.ExamId == examId && a.StudentId == requestingUserId && a.Status != AttemptStatus.InProgress)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync(ct);

        var gradeRecord = await _db.GradeRecords
            .FirstOrDefaultAsync(g => g.ExamId == examId && g.StudentId == requestingUserId, ct);

        var summaries = attempts
            .Select((a, index) => new ExamAttemptSummaryDto(
                AttemptId: a.Id,
                StudentId: a.StudentId,
                StudentName: "", // Self view doesn't need name
                StudentDisplayId: "",
                AttemptNumber: attempts.Count - index, // descending by date => first entry is latest
                Status: a.Status,
                StartedAt: a.StartedAt,
                SubmittedAt: a.SubmittedAt,
                Score: a.Score,
                Passed: a.Score.HasValue ? a.Score >= exam.PassScore : null,
                IsGradePublished: gradeRecord?.IsPublished ?? false,
                Notes: null // Notes not shown to students
            )).ToList();

        return ApiResponse<IReadOnlyList<ExamAttemptSummaryDto>>.Ok(summaries);
    }

    // ── Get All Attempts for Exam (Tutor Panel) ───────────────────────────────

    /// <inheritdoc/>
    public async Task<ApiResponse<IReadOnlyList<ExamAttemptSummaryDto>>> GetExamAttemptsAsync(
        Guid examId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var exam = await _db.Exams
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == examId, ct)
            ?? throw new NotFoundException("Exam", examId);

        if (requestingUserRole == "Tutor" && exam.Course?.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only view results for exams in your courses.");

        var attempts = await _db.ExamAttempts
            .Include(a => a.Student)
            .Where(a => a.ExamId == examId && a.Status != AttemptStatus.InProgress)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync(ct);

        // Get published status for each student
        var gradeRecords = await _db.GradeRecords
            .Where(g => g.ExamId == examId)
            .ToListAsync(ct);

        // Group by student to compute attempt numbers
        var studentAttemptGroups = attempts
            .GroupBy(a => a.StudentId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(a => a.StartedAt).ToList());

        var summaries = attempts.Select(a =>
        {
            var student = a.Student;
            var grade = gradeRecords.FirstOrDefault(g => g.StudentId == a.StudentId);
            var studentAttempts = studentAttemptGroups[a.StudentId];
            int attemptNumber = studentAttempts.IndexOf(a) + 1;
            return new ExamAttemptSummaryDto(
                AttemptId: a.Id,
                StudentId: a.StudentId,
                StudentName: student != null ? $"{student.FirstName} {student.LastName}" : "Unknown",
                StudentDisplayId: student?.StudentId ?? string.Empty,
                AttemptNumber: attemptNumber,
                Status: a.Status,
                StartedAt: a.StartedAt,
                SubmittedAt: a.SubmittedAt,
                Score: a.Score,
                Passed: a.Score.HasValue ? a.Score >= exam.PassScore : null,
                IsGradePublished: grade?.IsPublished ?? false,
                Notes: a.Notes
            );
        }).ToList();

        return ApiResponse<IReadOnlyList<ExamAttemptSummaryDto>>.Ok(summaries);
    }

    // ── Grade Short Answers ───────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<ApiResponse<string>> GradeShortAnswersAsync(
        Guid attemptId,
        GradeShortAnswerRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var attempt = await _db.ExamAttempts
            .Include(a => a.Exam)
                .ThenInclude(e => e!.Course)
            .Include(a => a.Answers)
            .Include(a => a.AttemptQuestions)
                .ThenInclude(aq => aq.Question)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct)
            ?? throw new NotFoundException("Exam Attempt", attemptId);

        var exam = attempt.Exam!;

        // Authorization
        if (requestingUserRole == "Tutor" && exam.Course?.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only grade attempts for exams in your courses.");

        if (attempt.Status != AttemptStatus.Submitted &&
            attempt.Status != AttemptStatus.ForceSubmitted &&
            attempt.Status != AttemptStatus.Graded)
            throw new BusinessRuleException(
                "Only attempts in Submitted, ForceSubmitted, or Graded status can have short answers graded.");

        // If already graded, allow re-grading (correction of grading mistake)
        bool isRegrading = attempt.Status == AttemptStatus.Graded;

        // Validate and apply grades
        var shortAnswerQuestions = attempt.AttemptQuestions
            .Where(aq => aq.Question?.Type == QuestionType.ShortAnswer)
            .Select(aq => aq.Question!)
            .ToList();

        decimal additionalPoints = 0;

        foreach (var gradeItem in request.Grades)
        {
            var question = shortAnswerQuestions.FirstOrDefault(q => q.Id == gradeItem.QuestionId)
                ?? throw new BusinessRuleException($"Question {gradeItem.QuestionId} is not a short-answer question in this attempt.");

            if (gradeItem.PointsAwarded < 0 || gradeItem.PointsAwarded > question.Points)
                throw new BusinessRuleException(
                    $"Points for question '{question.QuestionText[..Math.Min(40, question.QuestionText.Length)]}...' must be between 0 and {question.Points}.");

            var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == gradeItem.QuestionId);
            if (answer != null)
            {
                answer.PointsAwarded = gradeItem.PointsAwarded;
                answer.IsCorrect = gradeItem.PointsAwarded > 0;
            }

            additionalPoints += gradeItem.PointsAwarded;
        }

        // Recalculate total score
        decimal totalPoints = attempt.AttemptQuestions.Sum(aq => aq.Question?.Points ?? 0);
        decimal autoPoints = attempt.Answers
            .Where(a =>
            {
                var q = attempt.AttemptQuestions.FirstOrDefault(aq => aq.QuestionId == a.QuestionId)?.Question;
                return q?.Type != QuestionType.ShortAnswer && a.PointsAwarded.HasValue;
            })
            .Sum(a => a.PointsAwarded!.Value);

        decimal finalPercentage = totalPoints > 0
            ? Math.Round(((autoPoints + additionalPoints) / totalPoints) * 100, 2)
            : 0;

        attempt.Score = finalPercentage;
        attempt.Status = AttemptStatus.Graded;
        await _db.SaveChangesAsync(ct);

        // Upsert GradeRecord
        await UpsertGradeRecordAsync(attempt, exam, finalPercentage, ct);

        string action = isRegrading ? "re-graded" : "graded";
        return ApiResponse<string>.Ok($"Short answers {action} successfully. Exam attempt is now fully graded.");
    }

    // ── Release Results ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<ApiResponse<string>> ReleaseResultsAsync(
        Guid examId,
        ReleaseResultsRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var exam = await _db.Exams
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == examId, ct)
            ?? throw new NotFoundException("Exam", examId);

        if (requestingUserRole == "Tutor" && exam.Course?.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only release results for exams in your courses.");

        if (exam.ResultVisibility == ResultVisibility.Immediate)
            throw new BusinessRuleException("Results for Immediate-visibility exams are released automatically on submission.");

        // Fetch grade records to publish
        var query = _db.GradeRecords.Where(g => g.ExamId == examId && !g.IsPublished);

        if (request.StudentIds?.Count > 0)
            query = query.Where(g => request.StudentIds.Contains(g.StudentId));

        var records = await query.ToListAsync(ct);

        if (!records.Any())
            return ApiResponse<string>.Ok("No unpublished results found to release.");

        foreach (var record in records)
        {
            record.IsPublished = true;
            record.PublishedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        // Notify each affected student
        var affectedAttempts = await _db.ExamAttempts
            .Where(a => a.ExamId == examId &&
                        records.Select(r => r.StudentId).Contains(a.StudentId) &&
                        a.Status != AttemptStatus.InProgress)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync(ct);

        // Only notify each student once (latest attempt)
        var notifiedStudents = new HashSet<Guid>();
        foreach (var attempt in affectedAttempts)
        {
            if (!notifiedStudents.Add(attempt.StudentId)) continue;

            bool passed = attempt.Score.HasValue && attempt.Score >= exam.PassScore;
            string resultText = attempt.Score.HasValue
                ? (passed ? "Passed" : "Not Passed")
                : "Awaiting review";

            await _notifications.TriggerNotificationAsync(
                attempt.StudentId,
                "Exam Result Released",
                $"Your result for '{exam.Title}' has been released. {resultText}",
                $"/exam-results/{attempt.Id}",
                NotificationType.ExamResultReleased,
                exam.Id,
                sendEmail: false,
                ct);
        }

        return ApiResponse<string>.Ok($"Results released for {records.Count} student(s).");
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Determines whether the result data (score + per-question review) is currently visible to the requesting user.
    /// For ManualRelease, checks gradeRecord.IsPublished (set when tutor calls ReleaseResults).
    /// </summary>
    private static bool DetermineResultVisibility(ExamAttempt attempt, Exam exam, string role, Guid requestingUserId, GradeRecord? gradeRecord = null)
    {
        // Tutors and Admins always see everything, UNLESS they are viewing their own attempt
        if ((role is "Tutor" or "Admin") && attempt.StudentId != requestingUserId) return true;

        // Attempt still in progress - nothing to show
        if (attempt.Status == AttemptStatus.InProgress) return false;

        return exam.ResultVisibility switch
        {
            ResultVisibility.Immediate => true,
            // ManualRelease: visible only after Tutor/Admin explicitly releases (IsPublished = true)
            ResultVisibility.ManualRelease => gradeRecord?.IsPublished == true,
            // Scheduled: not implemented - treat as ManualRelease (hidden until manual release)
            ResultVisibility.Scheduled => gradeRecord?.IsPublished == true,
            _ => false
        };
    }

    /// <summary>
    /// Builds the per-question review for the result page.
    /// Correct answers are only included for MCQ/TrueFalse.
    /// </summary>
    private static IReadOnlyList<QuestionReviewDto> BuildQuestionReviews(ExamAttempt attempt, Exam exam)
    {
        // Show correct answers for MCQ/TF only on Immediate exams
        bool showCorrectAnswers = exam.ResultVisibility == ResultVisibility.Immediate;

        return attempt.AttemptQuestions
            .OrderBy(aq => aq.OrderIndex)
            .Select(aq =>
            {
                var q = aq.Question!;
                var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == q.Id);

                var selectedOption = q.Options.FirstOrDefault(o => o.Id == answer?.SelectedOptionId);
                var correctOption = showCorrectAnswers && q.Type != QuestionType.ShortAnswer
                    ? q.Options.FirstOrDefault(o => o.IsCorrect)
                    : null;

                return new QuestionReviewDto(
                    QuestionId: q.Id,
                    QuestionText: q.QuestionText,
                    ImageUrl: q.ImageUrl,
                    Type: q.Type,
                    Points: q.Points,
                    PointsAwarded: answer?.PointsAwarded,
                    IsCorrect: answer?.IsCorrect,
                    SelectedOptionId: answer?.SelectedOptionId,
                    SelectedOptionText: selectedOption?.OptionText,
                    CorrectOptionId: correctOption?.Id,
                    CorrectOptionText: correctOption?.OptionText,
                    TextAnswer: answer?.TextAnswer,
                    RequiresManualGrading: q.Type == QuestionType.ShortAnswer
                );
            })
            .ToList();
    }

    /// <summary>
    /// Returns true when the student is eligible to request a re-attempt:
    /// failed the exam, exhausted all allowed attempts, and has no pending request.
    /// </summary>
    private async Task<bool> CanRequestReattemptAsync(ExamAttempt latestAttempt, Exam exam, CancellationToken ct)
    {
        // Student passed - no re-attempt needed
        if (latestAttempt.Score.HasValue && latestAttempt.Score >= exam.PassScore)
            return false;

        // Count completed attempts
        int completedAttempts = await _db.ExamAttempts
            .CountAsync(a => a.ExamId == exam.Id &&
                             a.StudentId == latestAttempt.StudentId &&
                             a.Status != AttemptStatus.InProgress, ct);

        int approvedReattempts = await _db.ReattemptRequests
            .CountAsync(r => r.ExamId == exam.Id &&
                             r.StudentId == latestAttempt.StudentId &&
                             r.Status == "Approved", ct);

        int totalAllowed = exam.MaxAttempts + approvedReattempts;

        // Attempts not yet exhausted - student can just try again
        if (completedAttempts < totalAllowed)
            return false;

        // Check for existing pending request
        bool hasPendingRequest = await _db.ReattemptRequests
            .AnyAsync(r => r.ExamId == exam.Id &&
                           r.StudentId == latestAttempt.StudentId &&
                           r.Status == "Pending", ct);

        return !hasPendingRequest;
    }

    /// <summary>
    /// Creates or updates the GradeRecord after short-answer grading is complete.
    /// </summary>
    private async Task UpsertGradeRecordAsync(ExamAttempt attempt, Exam exam, decimal percentage, CancellationToken ct)
    {
        bool publishImmediately = exam.ResultVisibility == ResultVisibility.Immediate;

        var existing = await _db.GradeRecords
            .FirstOrDefaultAsync(g => g.ExamId == exam.Id && g.StudentId == attempt.StudentId, ct);

        if (existing == null)
        {
            _db.GradeRecords.Add(new GradeRecord
            {
                StudentId = attempt.StudentId,
                CourseId = exam.CourseId,
                ExamId = exam.Id,
                Type = GradeType.Exam,
                Score = percentage,
                MaxScore = 100,
                Weight = exam.Weight,
                IsPublished = publishImmediately,
                PublishedAt = publishImmediately ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Score = percentage;
            existing.UpdatedAt = DateTime.UtcNow;
            if (publishImmediately && !existing.IsPublished)
            {
                existing.IsPublished = true;
                existing.PublishedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);

        // Notify student if Immediate
        if (publishImmediately)
        {
            bool passed = percentage >= exam.PassScore;
            await _notifications.TriggerNotificationAsync(
                attempt.StudentId,
                "Exam Result Available",
                $"Your result for '{exam.Title}' is ready. Score: {percentage:F1}%",
                $"/exam-results/{attempt.Id}",
                NotificationType.ExamResultReleased,
                exam.Id,
                sendEmail: false,
                ct);
        }
    }

    // ── Export Results to CSV ──────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<byte[]> ExportResultsToCsvAsync(
        Guid examId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var summariesResponse = await GetExamAttemptsAsync(examId, requestingUserId, requestingUserRole, ct);
        var attempts = summariesResponse.Data;

        var sb = new System.Text.StringBuilder();
        // CSV Header
        sb.AppendLine("StudentName,StudentId,AttemptNumber,Status,SubmittedAt,Score,Passed,IsGradePublished");

        if (attempts != null)
        {
            foreach (var a in attempts)
            {
            var submittedAt = a.SubmittedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
            var score = a.Score.HasValue ? a.Score.Value.ToString("F2") : "Pending";
            var passed = a.Passed.HasValue ? (a.Passed.Value ? "Yes" : "No") : "N/A";
            var published = a.IsGradePublished ? "Yes" : "No";

            sb.AppendLine($"\"{a.StudentName}\",\"{a.StudentDisplayId}\",{a.AttemptNumber},\"{a.Status}\",\"{submittedAt}\",\"{score}\",\"{passed}\",\"{published}\"");
            }
        }

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }
}
