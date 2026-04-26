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
    DateTime? ScheduledReleaseAt
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
    DateTime? ScheduledReleaseAt
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
    DateTime? ScheduledReleaseAt,
    int QuestionCount,
    DateTime CreatedAt
);

/// <summary>Full exam detail including all question metadata (no correct answers for students).</summary>
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
    DateTime? ScheduledReleaseAt,
    int QuestionCount,
    string CreatedByName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
