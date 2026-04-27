namespace SHIELDON.Application.Features.Exams.DTOs;

// ── Request DTOs ─────────────────────────────────────────────────────────────

/// <summary>Student submits a re-attempt request for an exam they failed.</summary>
public record SubmitReattemptRequest(string Justification);

/// <summary>Admin/Tutor reviews (approves or rejects) a pending re-attempt request.</summary>
public record ReviewReattemptRequest(bool Approved, string? RejectionReason = null);

/// <summary>Query parameters for listing re-attempt requests.</summary>
public record ReattemptQueryParams(
    int Page = 1,
    int PageSize = 10,
    string? Status = null,
    Guid? ExamId = null,
    Guid? CourseId = null
);

// ── Response DTOs ─────────────────────────────────────────────────────────────

/// <summary>Full re-attempt request detail for Admin/Tutor review tables.</summary>
public record ReattemptRequestResponse(
    Guid Id,
    Guid ExamId,
    string ExamTitle,
    Guid CourseId,
    string CourseTitle,
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    string? StudentDisplayId,
    string Justification,
    string Status,
    int AttemptsMade,
    int MaxAttempts,
    DateTime RequestedAt,
    DateTime? ReviewedAt,
    string? ReviewedByName,
    string? RejectionReason
);

/// <summary>Student-facing view of their own re-attempt request.</summary>
public record StudentReattemptStatusResponse(
    Guid Id,
    Guid ExamId,
    string ExamTitle,
    string Justification,
    string Status,
    int AttemptsMade,
    int MaxAttempts,
    DateTime RequestedAt,
    DateTime? ReviewedAt,
    string? RejectionReason
);
