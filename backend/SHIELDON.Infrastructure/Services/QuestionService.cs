using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Features.Exams.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Implements the centralized question bank per course.
///
/// Security contract:
///   - Correct answers (IsCorrect) are NEVER returned in student-facing calls.
///   - Only the assigned Tutor or any Admin can modify the bank.
///   - Questions are course-scoped - no exam locking required.
/// </summary>
public class QuestionService : IQuestionService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    private static readonly HashSet<string> AllowedImageExtensions =
        [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

    public QuestionService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ── Add Question ──────────────────────────────────────────────────────────

    public async Task<QuestionResponse> AddQuestionAsync(
        Guid courseId,
        AddQuestionRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var course = await LoadCourseAsync(courseId, ct);
        AuthorizeForCourse(course, requestingUserId, requestingUserRole);

        if (!Enum.TryParse<QuestionType>(request.Type, ignoreCase: true, out var questionType))
            throw new BusinessRuleException($"Invalid question type '{request.Type}'. Use: MCQ, TrueFalse, ShortAnswer.");

        if (request.Points <= 0)
            throw new BusinessRuleException("Points must be greater than 0.");

        if (string.IsNullOrWhiteSpace(request.QuestionText))
            throw new BusinessRuleException("Question text cannot be empty.");

        // Assign next OrderIndex in this course's bank
        var nextOrder = await _db.ExamQuestions
            .Where(q => q.CourseId == courseId)
            .MaxAsync(q => (int?)q.OrderIndex, ct) ?? 0;
        nextOrder++;

        var question = new ExamQuestion
        {
            CourseId = courseId,
            QuestionText = request.QuestionText.Trim(),
            Type = questionType,
            Points = request.Points,
            OrderIndex = nextOrder,
            IsRandomized = request.IsRandomized,
            CreatedByUserId = requestingUserId
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
                    throw new BusinessRuleException("ShortAnswer questions do not have options - they are manually graded.");
                break;
        }

        await _db.SaveChangesAsync(ct);

        // Reload with options for the response
        await _db.Entry(question).Collection(q => q.Options).LoadAsync(ct);
        return MapToResponse(question, includeIsCorrect: true);
    }

    // ── Get Questions ─────────────────────────────────────────────────────────

    public async Task<List<QuestionResponse>> GetQuestionsAsync(
        Guid courseId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var course = await LoadCourseAsync(courseId, ct);
        AuthorizeForCourse(course, requestingUserId, requestingUserRole);

        var questions = await _db.ExamQuestions
            .Include(q => q.Options)
            .Where(q => q.CourseId == courseId)
            .OrderBy(q => q.OrderIndex)
            .AsNoTracking()
            .ToListAsync(ct);

        return questions.Select(q => MapToResponse(q, includeIsCorrect: true)).ToList();
    }

    // ── Get Bank Counts ───────────────────────────────────────────────────────

    public async Task<Dictionary<string, int>> GetBankCountsAsync(
        Guid courseId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var course = await LoadCourseAsync(courseId, ct);
        AuthorizeForCourse(course, requestingUserId, requestingUserRole);

        var counts = await _db.ExamQuestions
            .Where(q => q.CourseId == courseId)
            .GroupBy(q => q.Type)
            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(ct);

        return counts.ToDictionary(x => x.Type, x => x.Count);
    }

    // ── Update Question ───────────────────────────────────────────────────────

    public async Task<QuestionResponse> UpdateQuestionAsync(
        Guid questionId,
        UpdateQuestionRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var question = await LoadQuestionAsync(questionId, ct);
        AuthorizeForCourse(question.Course!, requestingUserId, requestingUserRole);

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

        if (request.Options is not null && question.Type == QuestionType.MCQ)
        {
            ValidateMcqOptions(request.Options);
            await _db.Entry(question).Collection(q => q.Options).LoadAsync(ct);
            _db.QuestionOptions.RemoveRange(question.Options);

            foreach (var opt in request.Options)
            {
                _db.QuestionOptions.Add(new QuestionOption
                {
                    QuestionId = question.Id,
                    OptionText = opt.OptionText.Trim(),
                    IsCorrect = opt.IsCorrect
                });
            }
        }
        else if (request.TrueFalseCorrectAnswer.HasValue && question.Type == QuestionType.TrueFalse)
        {
            await _db.Entry(question).Collection(q => q.Options).LoadAsync(ct);
            var trueOpt = question.Options.FirstOrDefault(o => o.OptionText == "True");
            var falseOpt = question.Options.FirstOrDefault(o => o.OptionText == "False");

            if (trueOpt != null && falseOpt != null)
            {
                trueOpt.IsCorrect = request.TrueFalseCorrectAnswer.Value;
                falseOpt.IsCorrect = !request.TrueFalseCorrectAnswer.Value;
            }
        }

        await _db.SaveChangesAsync(ct);

        // Refresh options safely
        question.Options = await _db.QuestionOptions.Where(o => o.QuestionId == question.Id).ToListAsync(ct);

        return MapToResponse(question, includeIsCorrect: true);
    }

    // ── Delete Question ───────────────────────────────────────────────────────

    public async Task DeleteQuestionAsync(
        Guid questionId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var question = await LoadQuestionAsync(questionId, ct);
        AuthorizeForCourse(question.Course!, requestingUserId, requestingUserRole);

        question.IsDeleted = true;
        question.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Re-normalize OrderIndex for remaining questions
        var remaining = await _db.ExamQuestions
            .Where(q => q.CourseId == question.CourseId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync(ct);

        for (int i = 0; i < remaining.Count; i++)
            remaining[i].OrderIndex = i + 1;

        await _db.SaveChangesAsync(ct);
    }

    // ── Reorder Questions ─────────────────────────────────────────────────────

    public async Task ReorderQuestionsAsync(
        Guid courseId,
        ReorderQuestionsRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var course = await LoadCourseAsync(courseId, ct);
        AuthorizeForCourse(course, requestingUserId, requestingUserRole);

        var questions = await _db.ExamQuestions
            .Where(q => q.CourseId == courseId)
            .ToListAsync(ct);

        if (request.Items.Count != questions.Count)
            throw new BusinessRuleException($"Reorder payload must include all {questions.Count} questions in this bank.");

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
        var question = await LoadQuestionAsync(questionId, ct);
        AuthorizeForCourse(question.Course!, requestingUserId, requestingUserRole);

        if (question.Type != QuestionType.MCQ)
            throw new BusinessRuleException("Options can only be added to MCQ questions.");

        if (string.IsNullOrWhiteSpace(request.OptionText))
            throw new BusinessRuleException("Option text cannot be empty.");

        await _db.Entry(question).Collection(q => q.Options).LoadAsync(ct);

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
                .ThenInclude(q => q!.Course)
            .Include(o => o.Question)
                .ThenInclude(q => q!.Options)
            .FirstOrDefaultAsync(o => o.Id == optionId, ct)
            ?? throw new NotFoundException("Option", optionId);

        AuthorizeForCourse(option.Question!.Course!, requestingUserId, requestingUserRole);

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
                .ThenInclude(q => q!.Course)
            .Include(o => o.Question)
                .ThenInclude(q => q!.Options)
            .FirstOrDefaultAsync(o => o.Id == optionId, ct)
            ?? throw new NotFoundException("Option", optionId);

        AuthorizeForCourse(option.Question!.Course!, requestingUserId, requestingUserRole);

        if (option.Question.Type != QuestionType.MCQ)
            throw new BusinessRuleException("Only MCQ options can be deleted individually.");

        if (option.Question.Options.Count <= 2)
            throw new BusinessRuleException("MCQ questions must have at least 2 options.");

        _db.QuestionOptions.Remove(option);
        await _db.SaveChangesAsync(ct);
    }

    // ── Image Upload ────────────────────────────────────────────────────────────

    public async Task<string> UploadQuestionImageAsync(
        Guid questionId,
        Stream imageStream,
        string fileName,
        long fileSize,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var question = await LoadQuestionAsync(questionId, ct);
        AuthorizeForCourse(question.Course!, requestingUserId, requestingUserRole);

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
            throw new BusinessRuleException($"Invalid file type '{ext}'. Allowed: {string.Join(", ", AllowedImageExtensions)}");

        if (fileSize > MaxImageSizeBytes)
            throw new BusinessRuleException("Image file is too large. Maximum allowed size is 5 MB.");

        // Delete old image if exists
        if (!string.IsNullOrEmpty(question.ImageUrl))
            DeleteImageFile(question.ImageUrl);

        var folder = Path.Combine(_env.WebRootPath, "Uploads", "questions");
        Directory.CreateDirectory(folder);
        var newFileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(folder, newFileName);

        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await imageStream.CopyToAsync(fileStream, ct);

        question.ImageUrl = $"/Uploads/questions/{newFileName}";
        await _db.SaveChangesAsync(ct);
        return question.ImageUrl;
    }

    public async Task DeleteQuestionImageAsync(
        Guid questionId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var question = await LoadQuestionAsync(questionId, ct);
        AuthorizeForCourse(question.Course!, requestingUserId, requestingUserRole);

        if (string.IsNullOrEmpty(question.ImageUrl))
            return; // No image to delete

        DeleteImageFile(question.ImageUrl);
        question.ImageUrl = null;
        await _db.SaveChangesAsync(ct);
    }

    private void DeleteImageFile(string relativeUrl)
    {
        try
        {
            var filePath = Path.Combine(_env.WebRootPath, relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch { /* swallow - file cleanup is best-effort */ }
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task<Course> LoadCourseAsync(Guid courseId, CancellationToken ct)
    {
        return await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new NotFoundException("Course", courseId);
    }

    private async Task<ExamQuestion> LoadQuestionAsync(Guid questionId, CancellationToken ct)
    {
        return await _db.ExamQuestions
            .Include(q => q.Course)
            .FirstOrDefaultAsync(q => q.Id == questionId, ct)
            ?? throw new NotFoundException("Question", questionId);
    }

    private static void AuthorizeForCourse(Course course, Guid userId, string role)
    {
        if (role == "Tutor" && course.AssignedTutorId != userId)
            throw new ForbiddenException("You can only manage questions for courses assigned to you.");
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
                includeIsCorrect ? o.IsCorrect : false))
            .ToList();

        return new QuestionResponse(
            Id: q.Id,
            CourseId: q.CourseId,
            QuestionText: q.QuestionText,
            ImageUrl: q.ImageUrl,
            Type: q.Type.ToString(),
            Points: q.Points,
            OrderIndex: q.OrderIndex,
            IsRandomized: q.IsRandomized,
            Options: options
        );
    }
}
