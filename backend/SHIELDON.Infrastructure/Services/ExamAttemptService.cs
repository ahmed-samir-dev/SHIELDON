using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Exams.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using System.Security.Cryptography;

namespace SHIELDON.Infrastructure.Services;

public class ExamAttemptService : IExamAttemptService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    public ExamAttemptService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<ApiResponse<StartExamResponse>> StartExamAsync(Guid examId, Guid studentId, CancellationToken ct = default)
    {
        var exam = await _db.Exams
            .Include(e => e.SelectionRules)
            .FirstOrDefaultAsync(e => e.Id == examId, ct)
            ?? throw new NotFoundException("Exam", examId);

        if (exam.Status != ExamStatus.Published)
            throw new BusinessRuleException("You cannot start an exam that is not published.");

        if (exam.ScheduledAt.HasValue && exam.ScheduledAt.Value > DateTime.UtcNow)
        {
            var scheduledFor = exam.ScheduledAt.Value.ToString("MMM d, yyyy, h:mm tt UTC");
            throw new BusinessRuleException($"This exam is scheduled to start on {scheduledFor}. Please wait.");
        }

        if (exam.ScheduledEndAt.HasValue && exam.ScheduledEndAt.Value < DateTime.UtcNow)
        {
            throw new BusinessRuleException("The deadline for this exam has passed. You can no longer start it.");
        }

        var isEnrolled = await _db.CourseEnrollments.AnyAsync(
            e => e.CourseId == exam.CourseId && e.StudentId == studentId && e.Status == CourseEnrollmentStatus.Approved, ct);
        if (!isEnrolled)
            throw new ForbiddenException("You must be enrolled in the course to take this exam.");

        if (!exam.SelectionRules.Any())
            throw new BusinessRuleException("This exam has no question selection rules.");

        // Check attempts made
        var completedAttempts = await _db.ExamAttempts
            .Where(a => a.ExamId == examId && a.StudentId == studentId && a.Status != AttemptStatus.InProgress)
            .ToListAsync(ct);

        int attemptsMade = completedAttempts.Count;
        int approvedReattempts = await _db.ReattemptRequests
            .CountAsync(r => r.ExamId == examId && r.StudentId == studentId && r.Status == "Approved", ct);

        int totalAllowed = exam.MaxAttempts + approvedReattempts;

        // Check for active attempt
        var activeAttempt = await _db.ExamAttempts
            .Include(a => a.Token)
            .Include(a => a.Answers)
            .Include(a => a.AttemptQuestions)
                .ThenInclude(aq => aq.Question)
                    .ThenInclude(q => q!.Options)
            .FirstOrDefaultAsync(a => a.ExamId == examId && a.StudentId == studentId && a.Status == AttemptStatus.InProgress, ct);

        if (activeAttempt != null)
            {
            if (activeAttempt.Token != null && activeAttempt.Token.ExpiresAt > DateTime.UtcNow)
                {
                // Resume existing
                return ApiResponse<StartExamResponse>.Ok(CreateStartResponse(activeAttempt, exam));
            }
            else
            {
                // Auto force-submit expired attempt
                await SubmitExamAsync(activeAttempt.Id, activeAttempt.Token?.Token ?? Guid.Empty, isForceSubmit: true, ct);
                attemptsMade++;
                }
            }

        if (attemptsMade >= totalAllowed)
            throw new BusinessRuleException($"You have exhausted all attempts for this exam. (Max: {totalAllowed})");

        // ── Draw questions from the bank (random selection per type) ──────────
        var selectedQuestions = new List<ExamQuestion>();

        foreach (var rule in exam.SelectionRules)
        {
            var bankQuestions = await _db.ExamQuestions
                .Include(q => q.Options)
                .Where(q => q.CourseId == exam.CourseId && q.Type == rule.QuestionType)
                .ToListAsync(ct);

            if (bankQuestions.Count < rule.Count)
                throw new BusinessRuleException(
                    $"Not enough {rule.QuestionType} questions in the bank. Need {rule.Count}, found {bankQuestions.Count}.");

            // Cryptographically random Fisher-Yates shuffle within each type bucket
            for (int i = bankQuestions.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (bankQuestions[i], bankQuestions[j]) = (bankQuestions[j], bankQuestions[i]);
            }

            selectedQuestions.AddRange(bankQuestions.Take(rule.Count));
        }

        // ── Final cross-type shuffle — ensures MCQ/TF/SA are interleaved randomly ──
        for (int i = selectedQuestions.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (selectedQuestions[i], selectedQuestions[j]) = (selectedQuestions[j], selectedQuestions[i]);
        }

        // Create new attempt
        var attempt = new ExamAttempt
        {
            ExamId = examId,
            StudentId = studentId,
            Status = AttemptStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        var token = new ExamToken
        {
            AttemptId = attempt.Id,
            ExpiresAt = DateTime.UtcNow.AddMinutes(exam.TimeLimit)
        };

        attempt.Token = token;
        _db.ExamAttempts.Add(attempt);

        // Save the snapshot of selected questions for this attempt
        int orderIdx = 1;
        foreach (var q in selectedQuestions)
        {
            _db.ExamAttemptQuestions.Add(new ExamAttemptQuestion
            {
                AttemptId = attempt.Id,
                QuestionId = q.Id,
                OrderIndex = orderIdx++
            });
        }

        await _db.SaveChangesAsync(ct);

        // Reload with options for the response
        await _db.Entry(attempt).Collection(a => a.AttemptQuestions).Query()
            .Include(aq => aq.Question)
                .ThenInclude(q => q!.Options)
            .LoadAsync(ct);

        return ApiResponse<StartExamResponse>.Ok(CreateStartResponse(attempt, exam));
    }

    public async Task<ApiResponse<string>> SaveAnswerAsync(Guid attemptId, Guid token, SaveAnswerRequest request, CancellationToken ct = default)
    {
        var attempt = await GetValidAttemptAsync(attemptId, token, ct);

        // Validate against the attempt snapshot (not the whole bank)
        var isValidQuestion = await _db.ExamAttemptQuestions
            .AnyAsync(aq => aq.AttemptId == attemptId && aq.QuestionId == request.QuestionId, ct);
        if (!isValidQuestion)
            throw new BusinessRuleException("Question not found in this exam attempt.");

        var existingAnswer = await _db.AttemptAnswers.FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.QuestionId == request.QuestionId, ct);

        if (existingAnswer == null)
        {
            _db.AttemptAnswers.Add(new AttemptAnswer
            {
                AttemptId = attemptId,
                QuestionId = request.QuestionId,
                SelectedOptionId = request.SelectedOptionId,
                TextAnswer = request.TextAnswer
            });
        }
        else
        {
            existingAnswer.SelectedOptionId = request.SelectedOptionId;
            existingAnswer.TextAnswer = request.TextAnswer;
        }

        await _db.SaveChangesAsync(ct);
        return ApiResponse<string>.Ok("Answer saved successfully.");
    }

    public async Task<ApiResponse<SubmitExamResponse>> SubmitExamAsync(Guid attemptId, Guid token, bool isForceSubmit = false, CancellationToken ct = default)
    {
        // Don't use GetValidAttemptAsync because we want to allow submit even if token is just expired (e.g., force submit)
        var attempt = await _db.ExamAttempts
            .Include(a => a.Token)
            .Include(a => a.Exam)
            .Include(a => a.AttemptQuestions)
                .ThenInclude(aq => aq.Question)
                    .ThenInclude(q => q!.Options)
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct)
            ?? throw new NotFoundException("Exam Attempt", attemptId);

        if (attempt.Status != AttemptStatus.InProgress)
            throw new BusinessRuleException("Attempt has already been submitted.");

        if (attempt.Token == null || attempt.Token.Token != token)
            throw new UnauthorizedException("Invalid token.");

        // If it's not a force submit, check expiry (allow 5 min grace total for UI lag)
        if (!isForceSubmit && attempt.Token.ExpiresAt < DateTime.UtcNow.AddMinutes(-5))
            throw new BusinessRuleException("Time limit has expired.");

        // Grade using the snapshot questions
        var snapshotQuestions = attempt.AttemptQuestions
            .Select(aq => aq.Question!)
            .ToList();

        decimal totalScore = 0;
        decimal maxPoints = snapshotQuestions.Sum(q => q.Points);
        bool requiresManualReview = false;

        foreach (var question in snapshotQuestions)
        {
            var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            if (answer == null) continue; // Unanswered

            if (question.Type == QuestionType.ShortAnswer)
            {
                requiresManualReview = true;
                continue;
            }

            var correctOptionId = question.Options.FirstOrDefault(o => o.IsCorrect)?.Id;
            if (answer.SelectedOptionId == correctOptionId && correctOptionId != null)
            {
                answer.IsCorrect = true;
                answer.PointsAwarded = question.Points;
                totalScore += question.Points;
            }
            else
            {
                answer.IsCorrect = false;
                answer.PointsAwarded = 0;
            }
        }

        decimal percentage = maxPoints > 0 ? (totalScore / maxPoints) * 100 : 0;
        
        attempt.Score = requiresManualReview ? null : percentage;
        attempt.Status = isForceSubmit ? AttemptStatus.ForceSubmitted : 
                         (requiresManualReview ? AttemptStatus.Submitted : AttemptStatus.Graded);
        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.Token.IsRevoked = true;

        await _db.SaveChangesAsync(ct);

        // ── Create / update GradeRecord & handle result visibility ────────────
        // Only create GradeRecord when the attempt is fully graded (not awaiting manual review)
        if (attempt.Status == AttemptStatus.Graded || attempt.Status == AttemptStatus.ForceSubmitted)
        {
            await CreateOrUpdateGradeRecordAsync(attempt, percentage, ct);
        }

        return ApiResponse<SubmitExamResponse>.Ok(new SubmitExamResponse(
            attempt.Id,
            attempt.Status,
            attempt.Score,
            attempt.Score.HasValue && attempt.Exam != null && attempt.Score >= attempt.Exam.PassScore,
            attempt.Exam!.ResultVisibility.ToString(),
            attempt.Exam.CourseId
        ));
    }

    /// <summary>
    /// Creates or refreshes the GradeRecord for this attempt and dispatches
    /// a result-released notification if ResultVisibility = Immediate.
    /// </summary>
    private async Task CreateOrUpdateGradeRecordAsync(ExamAttempt attempt, decimal percentage, CancellationToken ct)
    {
        var exam = attempt.Exam!;
        bool publishImmediately = exam.ResultVisibility == ResultVisibility.Immediate;

        // Upsert: only one GradeRecord per student per exam
        var existing = await _db.GradeRecords
            .FirstOrDefaultAsync(g => g.ExamId == exam.Id && g.StudentId == attempt.StudentId, ct);

        if (existing == null)
        {
            var grade = new GradeRecord
            {
                StudentId = attempt.StudentId,
                CourseId = exam.CourseId,
                ExamId = exam.Id,
                Type = GradeType.Exam,
                Score = Math.Round(percentage, 2),
                MaxScore = 100,
                Weight = exam.Weight,
                IsPublished = publishImmediately,
                PublishedAt = publishImmediately ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.GradeRecords.Add(grade);
        }
        else
        {
            // Re-attempt: update with the latest score
            existing.Score = Math.Round(percentage, 2);
            existing.UpdatedAt = DateTime.UtcNow;
            if (publishImmediately && !existing.IsPublished)
            {
                existing.IsPublished = true;
                existing.PublishedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);

        // Notify student immediately only for Immediate visibility
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
                sendEmail: true,
                ct);
        }
    }

    private async Task<ExamAttempt> GetValidAttemptAsync(Guid attemptId, Guid token, CancellationToken ct)
    {
        var attempt = await _db.ExamAttempts
            .Include(a => a.Token)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct)
            ?? throw new NotFoundException("Exam Attempt", attemptId);

        if (attempt.Status != AttemptStatus.InProgress)
            throw new BusinessRuleException("Attempt has already been submitted.");

        if (attempt.Token == null || attempt.Token.Token != token || attempt.Token.IsRevoked)
            throw new UnauthorizedException("Invalid or revoked token.");

        if (attempt.Token.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Token has expired.");

        return attempt;
    }

    private StartExamResponse CreateStartResponse(ExamAttempt attempt, Exam exam)
    {
        // Read questions from the snapshot, ordered by the attempt's OrderIndex
        var snapshotQuestions = attempt.AttemptQuestions
            .OrderBy(aq => aq.OrderIndex)
            .Select(aq => aq.Question!)
            .ToList();

        var finalOrder = new List<StudentQuestionDto>();

        foreach (var q in snapshotQuestions)
        {
            var maskedOptions = q.Options
                .OrderBy(o => o.Id)
                .Select(o => new StudentOptionDto(o.Id, o.OptionText))
                .ToList();

            // Shuffle MCQ options using cryptographic randomness
            if (q.Type == QuestionType.MCQ && q.IsRandomized)
            {
                var arr = maskedOptions.ToArray();
                for (int i = arr.Length - 1; i > 0; i--)
                {
                    int j = RandomNumberGenerator.GetInt32(i + 1);
                    (arr[i], arr[j]) = (arr[j], arr[i]);
                }
                maskedOptions = arr.ToList();
            }

            finalOrder.Add(new StudentQuestionDto(q.Id, q.QuestionText, q.Type, q.Points, maskedOptions));
        }

        var savedAnswers = attempt.Answers?.Select(a => new SavedAnswerDto(
            a.QuestionId,
            a.SelectedOptionId,
            a.TextAnswer
        )).ToList() ?? new List<SavedAnswerDto>();

        return new StartExamResponse(
            attempt.Id,
            attempt.Token!.Token,
            exam.TimeLimit,
            exam.PassScore,
            attempt.Token.ExpiresAt,
            finalOrder,
            savedAnswers,
            exam.CourseId,
            exam.ResultVisibility.ToString()
        );
    }
}
