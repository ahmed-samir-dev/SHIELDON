using SHIELDON.Domain.Enums;

namespace SHIELDON.Application.Features.Exams.DTOs;

// ── Question Bank Request DTOs ────────────────────────────────────────────────

/// <summary>
/// Tutor/Admin adds a question to the course question bank.
/// Type determines which validation rules apply:
///   - MCQ: Options required, exactly 1 must be IsCorrect
///   - TrueFalse: Options auto-created ("True"/"False"), IsCorrect on request body selects which one
///   - ShortAnswer: No options; manually graded by Tutor later
/// </summary>
public record AddQuestionRequest(
    string QuestionText,
    string Type,           // "MCQ" | "TrueFalse" | "ShortAnswer"
    decimal Points,
    bool IsRandomized = true,
    List<AddOptionRequest>? Options = null,
    bool? TrueFalseCorrectAnswer = null
);

/// <summary>Updates a bank question. Only the fields provided are changed.</summary>
public record UpdateQuestionRequest(
    string? QuestionText,
    decimal? Points,
    bool? IsRandomized,
    List<AddOptionRequest>? Options = null,
    bool? TrueFalseCorrectAnswer = null
);

/// <summary>Bulk reorder payload - send all questions with their new OrderIndex values.</summary>
public record ReorderQuestionsRequest(List<QuestionOrderItem> Items);
public record QuestionOrderItem(Guid QuestionId, int OrderIndex);

/// <summary>Add a single option to an MCQ question.</summary>
public record AddOptionRequest(string OptionText, bool IsCorrect);

/// <summary>Update an existing option's text or correct status.</summary>
public record UpdateOptionRequest(string? OptionText, bool? IsCorrect);

// ── Exam Selection Rule DTOs ──────────────────────────────────────────────────

/// <summary>
/// Defines how many questions of a given type the exam should draw from the bank.
/// </summary>
public record ExamSelectionRuleRequest(
    string QuestionType,   // "MCQ" | "TrueFalse" | "ShortAnswer"
    int Count
);

/// <summary>Selection rule as returned in exam responses.</summary>
public record ExamSelectionRuleResponse(
    Guid Id,
    string QuestionType,
    int Count
);

// ── Question Bank Response DTOs ───────────────────────────────────────────────

/// <summary>
/// Full question detail - returned to Tutor/Admin.
/// Options include IsCorrect.
/// Students NEVER receive IsCorrect (filtered at service level).
/// </summary>
public record QuestionResponse(
    Guid Id,
    Guid CourseId,         // ← was ExamId; now course-scoped
    string QuestionText,
    string Type,
    decimal Points,
    int OrderIndex,
    bool IsRandomized,
    List<OptionResponse> Options
);

/// <summary>
/// Option response for Tutor/Admin (includes IsCorrect).
/// For student-facing responses, IsCorrect is always set to false.
/// </summary>
public record OptionResponse(
    Guid Id,
    string OptionText,
    bool IsCorrect
);

/// <summary>Summary of a bank question for list views (no options).</summary>
public record QuestionSummaryResponse(
    Guid Id,
    Guid CourseId,
    string QuestionText,
    string Type,
    decimal Points,
    int OrderIndex
);
