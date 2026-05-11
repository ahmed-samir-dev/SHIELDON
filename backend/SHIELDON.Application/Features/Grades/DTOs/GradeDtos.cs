namespace SHIELDON.Application.Features.Grades.DTOs;

// ── Request DTOs ─────────────────────────────────────────────────────────────

/// <summary>Query parameters for listing grade records in a course.</summary>
public record GradeQueryParams(
    int Page = 1,
    int PageSize = 20,
    string? Type = null,          // "Exam" | "Assignment" | null (all)
    string? Status = null,        // "Published" | "Unpublished" | null (all)
    string? SearchTerm = null     // Filter by student name or display ID
);

/// <summary>Tutor/Admin updates weight, score override, or notes on a single grade record.</summary>
public record UpdateGradeRequest(
    decimal? Score,
    string? Notes
);

/// <summary>Publish individual or all grade records in a course.</summary>
public record BulkPublishRequest(
    List<Guid>? GradeIds = null,  // null = publish all unpublished in course
    bool PublishAll = false
);

// ── Response DTOs ─────────────────────────────────────────────────────────────

/// <summary>
/// One grade record row as returned in the Grade Management Panel.
/// WeightedScore = (Score / MaxScore) * Weight.
/// </summary>
public record GradeItemResponse(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string? StudentDisplayId,
    string StudentEmail,
    Guid CourseId,
    Guid? ExamId,
    string? ExamTitle,
    Guid? AssignmentId,
    string? AssignmentTitle,
    string Type,              // "Exam" | "Assignment"
    decimal Score,
    decimal MaxScore,
    decimal Weight,
    decimal WeightedScore,    // Computed: (Score / MaxScore) * Weight
    bool IsPublished,
    DateTime? PublishedAt,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Aggregated view per student for the course grade summary table.
/// Includes all grade items and a computed final weighted score.
/// </summary>
public record CourseGradeSummaryResponse(
    Guid StudentId,
    string StudentName,
    string? StudentDisplayId,
    string StudentEmail,
    IReadOnlyList<GradeItemResponse> Grades,
    decimal TotalWeightAssigned,   // Sum of all Weight values (should sum to 100 ideally)
    decimal? FinalWeightedScore    // Sum of all WeightedScore values
);

/// <summary>
/// Student-facing grade item (only published grades are returned).
/// </summary>
public record MyGradeItemResponse(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    Guid? ExamId,
    string? ExamTitle,
    Guid? AssignmentId,
    string? AssignmentTitle,
    string Type,
    decimal Score,
    decimal MaxScore,
    decimal Weight,
    decimal WeightedScore,
    DateTime? PublishedAt
);
