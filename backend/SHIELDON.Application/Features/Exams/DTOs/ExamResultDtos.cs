using SHIELDON.Domain.Enums;

namespace SHIELDON.Application.Features.Exams.DTOs;

// ── Student Result Response ───────────────────────────────────────────────────

/// <summary>
/// Full result returned to a student after grading is available.
/// </summary>
public record ExamResultResponse(
    Guid AttemptId,
    Guid ExamId,
    Guid CourseId,
    string ExamTitle,
    string CourseTitle,
    AttemptStatus Status,
    DateTime StartedAt,
    DateTime? SubmittedAt,
    decimal? Score,
    decimal PassScore,
    bool? Passed,
    bool IsPublished,
    /// <summary>Whether the result is currently visible to the student.</summary>
    bool ResultVisible,
    /// <summary>Per-question review list. null if result not yet released.</summary>
    IReadOnlyList<QuestionReviewDto>? QuestionReviews,
    /// <summary>True if student failed, attempts exhausted, and no pending re-attempt.</summary>
    bool CanRequestReattempt
);


/// <summary>
/// Per-question review item shown on the student result page.
/// IsCorrect and CorrectOptionId are populated only for MCQ/TrueFalse on Immediate exams.
/// </summary>
public record QuestionReviewDto(
    Guid QuestionId,
    string QuestionText,
    /// <summary>Optional image URL attached to this question, shown during result review.</summary>
    string? ImageUrl,
    QuestionType Type,
    decimal Points,
    decimal? PointsAwarded,
    bool? IsCorrect,
    Guid? SelectedOptionId,
    string? SelectedOptionText,
    /// <summary>Only populated for MCQ/TrueFalse when result is immediately visible.</summary>
    Guid? CorrectOptionId,
    string? CorrectOptionText,
    string? TextAnswer,
    bool RequiresManualGrading
);

// ── Tutor Results Panel ───────────────────────────────────────────────────────

/// <summary>
/// Row in the tutor's exam attempts table.
/// </summary>
public record ExamAttemptSummaryDto(
    Guid AttemptId,
    Guid StudentId,
    string StudentName,
    string StudentDisplayId,
    /// <summary>Which attempt number this is (1st, 2nd, 3rd …) for this student on this exam.</summary>
    int AttemptNumber,
    AttemptStatus Status,
    DateTime StartedAt,
    DateTime? SubmittedAt,
    decimal? Score,
    bool? Passed,
    bool IsGradePublished,
    /// <summary>
    /// System-generated note. Set when the question bank didn't have enough unseen questions
    /// for a fully distinct retake — some previously-seen questions may have appeared.
    /// </summary>
    string? Notes
);

// ── Short Answer Grading ──────────────────────────────────────────────────────

public class GradeShortAnswerRequest
{
    /// <summary>Map of QuestionId → PointsAwarded (0 to Question.Points).</summary>
    public required List<ShortAnswerGradeItem> Grades { get; set; }
}

public class ShortAnswerGradeItem
{
    public Guid QuestionId { get; set; }
    public decimal PointsAwarded { get; set; }
}

// ── Release Results ───────────────────────────────────────────────────────────

/// <summary>
/// Tutor/Admin releases results for all attempts on an exam.
/// </summary>
public class ReleaseResultsRequest
{
    /// <summary>Optional: release only specific students. If null, releases all.</summary>
    public List<Guid>? StudentIds { get; set; }
}
