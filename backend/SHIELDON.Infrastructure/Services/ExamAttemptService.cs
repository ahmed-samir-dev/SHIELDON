using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Exams.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

public class ExamAttemptService : IExamAttemptService
{
    private readonly AppDbContext _db;

    public ExamAttemptService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<StartExamResponse>> StartExamAsync(Guid examId, Guid studentId, CancellationToken ct = default)
    {
        var exam = await _db.Exams
            .Include(e => e.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == examId, ct)
            ?? throw new NotFoundException("Exam", examId);

        if (exam.Status != ExamStatus.Published)
            throw new BusinessRuleException("You cannot start an exam that is not published.");

        var isEnrolled = await _db.CourseEnrollments.AnyAsync(
            e => e.CourseId == exam.CourseId && e.StudentId == studentId && e.Status == CourseEnrollmentStatus.Approved, ct);
        if (!isEnrolled)
            throw new ForbiddenException("You must be enrolled in the course to take this exam.");

        if (exam.Questions.Count == 0)
            throw new BusinessRuleException("This exam has no questions.");

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
            ExpiresAt = DateTime.UtcNow.AddMinutes(exam.TimeLimit).AddMinutes(2) // 2 min grace period
        };

        attempt.Token = token;
        _db.ExamAttempts.Add(attempt);
        await _db.SaveChangesAsync(ct);

        return ApiResponse<StartExamResponse>.Ok(CreateStartResponse(attempt, exam));
    }

    public async Task<ApiResponse<string>> SaveAnswerAsync(Guid attemptId, Guid token, SaveAnswerRequest request, CancellationToken ct = default)
    {
        var attempt = await GetValidAttemptAsync(attemptId, token, ct);

        var question = await _db.ExamQuestions.FirstOrDefaultAsync(q => q.Id == request.QuestionId && q.ExamId == attempt.ExamId, ct)
            ?? throw new BusinessRuleException("Question not found in this exam.");

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
                .ThenInclude(e => e!.Questions)
                    .ThenInclude(q => q.Options)
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

        // Grade MCQ/TF
        decimal totalScore = 0;
        decimal maxPoints = attempt.Exam!.Questions.Sum(q => q.Points);
        bool requiresManualReview = false;

        foreach (var question in attempt.Exam.Questions)
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

        return ApiResponse<SubmitExamResponse>.Ok(new SubmitExamResponse(
            attempt.Id,
            attempt.Status,
            attempt.Score,
            attempt.Score >= attempt.Exam.PassScore
        ));
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
        var ordered = exam.Questions.OrderBy(q => q.OrderIndex).ToList();
        var randomQuestions = ordered.Where(q => q.IsRandomized).ToList();
        var random = new Random(attempt.Id.GetHashCode());

        for (int i = randomQuestions.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            var temp = randomQuestions[i];
            randomQuestions[i] = randomQuestions[j];
            randomQuestions[j] = temp;
        }

        int rIndex = 0;
        var finalOrder = new List<StudentQuestionDto>();
        
        foreach (var q in ordered)
        {
            var qToAdd = q.IsRandomized ? randomQuestions[rIndex++] : q;
            
            // Mask options (remove IsCorrect) and shuffle options too if you want, but for now just mask
            var maskedOptions = qToAdd.Options
                .OrderBy(o => o.Id) // Stable sort for options, could shuffle with the same random seed if desired
                .Select(o => new StudentOptionDto(o.Id, o.OptionText))
                .ToList();

            // Optional: Shuffle options for MCQ deterministically
            if (qToAdd.Type == QuestionType.MCQ && qToAdd.IsRandomized)
            {
                var optionsArray = maskedOptions.ToArray();
                for (int i = optionsArray.Length - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    var temp = optionsArray[i];
                    optionsArray[i] = optionsArray[j];
                    optionsArray[j] = temp;
                }
                maskedOptions = optionsArray.ToList();
            }

            finalOrder.Add(new StudentQuestionDto(
                qToAdd.Id,
                qToAdd.QuestionText,
                qToAdd.Type,
                qToAdd.Points,
                maskedOptions
            ));
        }

        return new StartExamResponse(
            attempt.Id,
            attempt.Token!.Token,
            exam.TimeLimit,
            exam.PassScore,
            attempt.Token.ExpiresAt,
            finalOrder
        );
    }
}
