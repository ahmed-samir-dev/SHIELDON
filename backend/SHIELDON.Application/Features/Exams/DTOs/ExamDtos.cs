namespace SHIELDON.Application.Features.Exams.DTOs;

// ── Request DTOs ─────────────────────────────────────────────────────────────

/// <summary>Tutor/Admin creates a new exam for a course.</summary>
public record CreateExamRequest(
    string Title,
    string? Instructions,
    int TimeLimit,
    int MaxAttempts,
    decimal PassScore,
    string ResultVisibility,
    DateTime? ScheduledAt,
    DateTime? ScheduledEndAt,
    DateTime? ScheduledReleaseAt,
    /// <summary>
    /// Defines how many questions of each type to draw from the course bank.
    /// e.g. [{QuestionType:"MCQ", Count:5}, {QuestionType:"TrueFalse", Count:3}]
    /// </summary>
    List<ExamSelectionRuleRequest>? SelectionRules = null
);

/// <summary>Tutor/Admin updates an existing exam. All fields are optional.</summary>
public record UpdateExamRequest(
    string? Title,
    string? Instructions,
    int? TimeLimit,
    int? MaxAttempts,
    decimal? PassScore,
    string? ResultVisibility,
    DateTime? ScheduledAt,
    DateTime? ScheduledEndAt,
    DateTime? ScheduledReleaseAt,
    List<ExamSelectionRuleRequest>? SelectionRules = null
);

/// <summary>Query parameters for listing exams in a course.</summary>
public record ExamQueryParams(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    string? Status = null
);

// ── Response DTOs ─────────────────────────────────────────────────────────────

/// <summary>Compact summary for exam list views.</summary>
public record ExamSummaryResponse(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string Title,
    string? Instructions,
    int TimeLimit,
    int MaxAttempts,
    decimal PassScore,
    string Status,
    string ResultVisibility,
    DateTime? ScheduledAt,
    DateTime? ScheduledEndAt,
    DateTime? ScheduledReleaseAt,
    /// <summary>Total questions already in the course bank.</summary>
    int BankQuestionCount,
    /// <summary>Selection rules that determine how many of each type are drawn.</summary>
    List<ExamSelectionRuleResponse> SelectionRules,
    DateTime CreatedAt
);

/// <summary>Full exam detail including selection rules and bank stats.</summary>
public record ExamDetailResponse(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string Title,
    string? Instructions,
    int TimeLimit,
    int MaxAttempts,
    decimal PassScore,
    string Status,
    string ResultVisibility,
    DateTime? ScheduledAt,
    DateTime? ScheduledEndAt,
    DateTime? ScheduledReleaseAt,
    int BankQuestionCount,
    List<ExamSelectionRuleResponse> SelectionRules,
    string CreatedByName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
