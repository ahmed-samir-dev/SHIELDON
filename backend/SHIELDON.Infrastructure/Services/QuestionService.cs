using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Features.Exams.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Implements the Question Bank for each exam.
///
/// Security contract:
///   - Correct answers (IsCorrect) are NEVER returned in student-facing calls.
///   - All write operations block Published/Closed exams.
///   - Tutor access is scoped to their assigned course only.
/// </summary>
public class QuestionService : IQuestionService
{
    private readonly AppDbContext _db;

    public QuestionService(AppDbContext db)
    {
        _db = db;
    }

    // ── Add Question ──────────────────────────────────────────────────────────

    public async Task<QuestionResponse> AddQuestionAsync(
        Guid examId,
        AddQuestionRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var exam = await LoadExamWithCourseAsync(examId, ct);
        AuthorizeForExam(exam, requestingUserId, requestingUserRole);
        EnsureExamIsDraft(exam);

        if (!Enum.TryParse<QuestionType>(request.Type, ignoreCase: true, out var questionType))
            throw new BusinessRuleException($"Invalid question type '{request.Type}'. Use: MCQ, TrueFalse, ShortAnswer.");

        if (request.Points <= 0)
            throw new BusinessRuleException("Points must be greater than 0.");

        if (string.IsNullOrWhiteSpace(request.QuestionText))
            throw new BusinessRuleException("Question text cannot be empty.");

        // Assign next OrderIndex
        var nextOrder = await _db.ExamQuestions
            .Where(q => q.ExamId == examId)
            .MaxAsync(q => (int?)q.OrderIndex, ct) ?? 0;
        nextOrder++;

        var question = new ExamQuestion
        {
            ExamId = examId,
            QuestionText = request.QuestionText.Trim(),
            Type = questionType,
            Points = request.Points,
            OrderIndex = nextOrder,
            IsRandomized = request.IsRandomized
        };

        _db.ExamQuestions.Add(question);

        // Build options per type
        switch (questionType)
        {
            case QuestionType.MCQ:
                ValidateMcqOptions(request.Options);
                foreach (var opt in request.Options!)
                {
                    _db.QuestionOptions.Add(new QuestionOption
                    {
                        QuestionId = question.Id,
                        OptionText = opt.OptionText.Trim(),
                        IsCorrect = opt.IsCorrect
                    });
                }
                break;

            case QuestionType.TrueFalse:
                if (!request.TrueFalseCorrectAnswer.HasValue)
                    throw new BusinessRuleException("TrueFalse questions require TrueFalseCorrectAnswer (true or false).");

                _db.QuestionOptions.Add(new QuestionOption
                {
                    QuestionId = question.Id,
                    OptionText = "True",
                    IsCorrect = request.TrueFalseCorrectAnswer.Value
                });
                _db.QuestionOptions.Add(new QuestionOption
                {
                    QuestionId = question.Id,
                    OptionText = "False",
                    IsCorrect = !request.TrueFalseCorrectAnswer.Value
                });
                break;

            case QuestionType.ShortAnswer:
                if (request.Options?.Count > 0)
                    throw new BusinessRuleException("ShortAnswer questions do not have options — they are manually graded.");
                break;
        }

        await _db.SaveChangesAsync(ct);

        // Reload with options for the response
        await _db.Entry(question).Collection(q => q.Options).LoadAsync(ct);
        return MapToResponse(question, includeIsCorrect: true);
    }

    // ── Get Questions ─────────────────────────────────────────────────────────

    public async Task<List<QuestionResponse>> GetQuestionsAsync(
        Guid examId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var exam = await _db.Exams
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == examId, ct)
            ?? throw new NotFoundException("Exam", examId);

        // Students can only see Published exams
        if (requestingUserRole == "Student" && exam.Status != ExamStatus.Published)
            throw new ForbiddenException("This exam is not available.");

        var questions = await _db.ExamQuestions
            .Include(q => q.Options)
            .Where(q => q.ExamId == examId)
            .OrderBy(q => q.OrderIndex)
            .AsNoTracking()
            .ToListAsync(ct);

        var includeIsCorrect = requestingUserRole is "Admin" or "Tutor";
        return questions.Select(q => MapToResponse(q, includeIsCorrect)).ToList();
    }

    // ── Update Question ───────────────────────────────────────────────────────

    public async Task<QuestionResponse> UpdateQuestionAsync(
        Guid questionId,
        UpdateQuestionRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var question = await LoadQuestionWithExamAsync(questionId, ct);
        AuthorizeForExam(question.Exam!, requestingUserId, requestingUserRole);
        EnsureExamIsDraft(question.Exam!);

        if (request.QuestionText is not null)
        {
            if (string.IsNullOrWhiteSpace(request.QuestionText))
                throw new BusinessRuleException("Question text cannot be empty.");
            question.QuestionText = request.QuestionText.Trim();
        }

        if (request.Points.HasValue)
        {
            if (request.Points.Value <= 0)
                throw new BusinessRuleException("Points must be greater than 0.");
            question.Points = request.Points.Value;
        }

        if (request.IsRandomized.HasValue)
            question.IsRandomized = request.IsRandomized.Value;

        await _db.SaveChangesAsync(ct);
        await _db.Entry(question).Collection(q => q.Options).LoadAsync(ct);
        return MapToResponse(question, includeIsCorrect: true);
    }

    // ── Delete Question ───────────────────────────────────────────────────────

    public async Task DeleteQuestionAsync(
        Guid questionId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var question = await LoadQuestionWithExamAsync(questionId, ct);
        AuthorizeForExam(question.Exam!, requestingUserId, requestingUserRole);
        EnsureExamIsDraft(question.Exam!);

        _db.ExamQuestions.Remove(question);
        await _db.SaveChangesAsync(ct);

        // Re-normalize OrderIndex for remaining questions
        var remaining = await _db.ExamQuestions
            .Where(q => q.ExamId == question.ExamId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync(ct);

        for (int i = 0; i < remaining.Count; i++)
            remaining[i].OrderIndex = i + 1;

        await _db.SaveChangesAsync(ct);
    }

    // ── Reorder Questions ─────────────────────────────────────────────────────

    public async Task ReorderQuestionsAsync(
        Guid examId,
        ReorderQuestionsRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var exam = await LoadExamWithCourseAsync(examId, ct);
        AuthorizeForExam(exam, requestingUserId, requestingUserRole);
        EnsureExamIsDraft(exam);

        var questions = await _db.ExamQuestions
            .Where(q => q.ExamId == examId)
            .ToListAsync(ct);

        if (request.Items.Count != questions.Count)
            throw new BusinessRuleException($"Reorder payload must include all {questions.Count} questions in this exam.");

        foreach (var item in request.Items)
        {
            var q = questions.FirstOrDefault(q => q.Id == item.QuestionId)
                ?? throw new NotFoundException("Question", item.QuestionId);
            q.OrderIndex = item.OrderIndex;
        }

        await _db.SaveChangesAsync(ct);
    }

    // ── Add Option ────────────────────────────────────────────────────────────

    public async Task<OptionResponse> AddOptionAsync(
        Guid questionId,
        AddOptionRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var question = await LoadQuestionWithExamAsync(questionId, ct);
        AuthorizeForExam(question.Exam!, requestingUserId, requestingUserRole);
        EnsureExamIsDraft(question.Exam!);

        if (question.Type != QuestionType.MCQ)
            throw new BusinessRuleException("Options can only be added to MCQ questions.");

        if (string.IsNullOrWhiteSpace(request.OptionText))
            throw new BusinessRuleException("Option text cannot be empty.");

        await _db.Entry(question).Collection(q => q.Options).LoadAsync(ct);

        // If marking this new option as correct, unmark all existing ones
        if (request.IsCorrect)
        {
            foreach (var existing in question.Options)
                existing.IsCorrect = false;
        }

        var option = new QuestionOption
        {
            QuestionId = questionId,
            OptionText = request.OptionText.Trim(),
            IsCorrect = request.IsCorrect
        };

        _db.QuestionOptions.Add(option);
        await _db.SaveChangesAsync(ct);
        return new OptionResponse(option.Id, option.OptionText, option.IsCorrect);
    }

    // ── Update Option ─────────────────────────────────────────────────────────

    public async Task<OptionResponse> UpdateOptionAsync(
        Guid optionId,
        UpdateOptionRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var option = await _db.QuestionOptions
            .Include(o => o.Question)
                .ThenInclude(q => q!.Exam)
                    .ThenInclude(e => e!.Course)
            .Include(o => o.Question)
                .ThenInclude(q => q!.Options)
            .FirstOrDefaultAsync(o => o.Id == optionId, ct)
            ?? throw new NotFoundException("Option", optionId);

        AuthorizeForExam(option.Question!.Exam!, requestingUserId, requestingUserRole);
        EnsureExamIsDraft(option.Question!.Exam!);

        if (option.Question.Type == QuestionType.TrueFalse)
            throw new BusinessRuleException("True/False options cannot be edited individually. Use the question update endpoint.");

        if (request.OptionText is not null)
        {
            if (string.IsNullOrWhiteSpace(request.OptionText))
                throw new BusinessRuleException("Option text cannot be empty.");
            option.OptionText = request.OptionText.Trim();
        }

        if (request.IsCorrect.HasValue && request.IsCorrect.Value)
        {
            // Move "correct" to this option — unmark all siblings
            foreach (var sibling in option.Question.Options)
                sibling.IsCorrect = false;
            option.IsCorrect = true;
        }

        await _db.SaveChangesAsync(ct);
        return new OptionResponse(option.Id, option.OptionText, option.IsCorrect);
    }

    // ── Delete Option ─────────────────────────────────────────────────────────

    public async Task DeleteOptionAsync(
        Guid optionId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var option = await _db.QuestionOptions
            .Include(o => o.Question)
                .ThenInclude(q => q!.Exam)
                    .ThenInclude(e => e!.Course)
            .Include(o => o.Question)
                .ThenInclude(q => q!.Options)
            .FirstOrDefaultAsync(o => o.Id == optionId, ct)
            ?? throw new NotFoundException("Option", optionId);

        AuthorizeForExam(option.Question!.Exam!, requestingUserId, requestingUserRole);
        EnsureExamIsDraft(option.Question!.Exam!);

        if (option.Question.Type != QuestionType.MCQ)
            throw new BusinessRuleException("Only MCQ options can be deleted individually.");

        if (option.Question.Options.Count <= 2)
            throw new BusinessRuleException("MCQ questions must have at least 2 options.");

        _db.QuestionOptions.Remove(option);
        await _db.SaveChangesAsync(ct);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task<Exam> LoadExamWithCourseAsync(Guid examId, CancellationToken ct)
    {
        return await _db.Exams
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == examId, ct)
            ?? throw new NotFoundException("Exam", examId);
    }

    private async Task<ExamQuestion> LoadQuestionWithExamAsync(Guid questionId, CancellationToken ct)
    {
        return await _db.ExamQuestions
            .Include(q => q.Exam)
                .ThenInclude(e => e!.Course)
            .FirstOrDefaultAsync(q => q.Id == questionId, ct)
            ?? throw new NotFoundException("Question", questionId);
    }

    private static void AuthorizeForExam(Exam exam, Guid userId, string role)
    {
        if (role == "Tutor" && exam.Course?.AssignedTutorId != userId)
            throw new ForbiddenException("You can only manage questions for exams in your assigned courses.");
    }

    private static void EnsureExamIsDraft(Exam exam)
    {
        if (exam.Status != ExamStatus.Draft)
            throw new BusinessRuleException(
                $"This exam is '{exam.Status}' and can no longer be modified. " +
                "Questions can only be added, edited, or deleted on Draft exams.");
    }

    private static void ValidateMcqOptions(List<AddOptionRequest>? options)
    {
        if (options is null || options.Count < 2)
            throw new BusinessRuleException("MCQ questions require at least 2 options.");

        var correctCount = options.Count(o => o.IsCorrect);
        if (correctCount != 1)
            throw new BusinessRuleException($"MCQ questions must have exactly 1 correct option. You marked {correctCount}.");

        if (options.Any(o => string.IsNullOrWhiteSpace(o.OptionText)))
            throw new BusinessRuleException("All option texts must be non-empty.");
    }

    private static QuestionResponse MapToResponse(ExamQuestion q, bool includeIsCorrect)
    {
        var options = q.Options
            .Select(o => new OptionResponse(
                o.Id,
                o.OptionText,
                includeIsCorrect ? o.IsCorrect : false))  // SECURITY: mask correct answer from students
            .ToList();

        return new QuestionResponse(
            Id: q.Id,
            ExamId: q.ExamId,
            QuestionText: q.QuestionText,
            Type: q.Type.ToString(),
            Points: q.Points,
            OrderIndex: q.OrderIndex,
            IsRandomized: q.IsRandomized,
            Options: options
        );
    }
}
