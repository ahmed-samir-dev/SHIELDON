namespace SHIELDON.Application.Features.Exams.DTOs;

// ── Request DTOs ───────────────────────────────────────────────────────────────────────

/// <summary>
/// Student submits a request. Sent as multipart/form-data.
/// If IsReopenRequest = true → student never entered the exam and wants re-open access.
/// If IsReopenRequest = false → student had an attempt (failed/expired) and wants a re-attempt.
/// AttachmentFile is optional proof (screenshot, document, etc.).
/// Max attachment size: 10 MB. Allowed: .jpg, .jpeg, .png, .pdf, .docx
/// </summary>
public record SubmitReattemptRequest(string Justification, bool IsReopenRequest = false);

/// <summary>
/// Admin/Tutor reviews (approves or rejects) a pending request.
/// If approving a Re-open Request, ExtensionHours must be set (24 or 48).
/// The backend will calculate ExtendedEndTime = UtcNow + ExtensionHours.
/// </summary>
public record ReviewReattemptRequest(bool Approved, string? RejectionReason = null, int? ExtensionHours = null);

/// <summary>Query parameters for listing re-attempt requests.</summary>
public record ReattemptQueryParams(
    int Page = 1,
    int PageSize = 10,
    string? Status = null,
    Guid? ExamId = null,
    Guid? CourseId = null,
    string? SearchTerm = null
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
    /// <summary>Relative URL to the student's proof attachment (if any).</summary>
    string? AttachmentUrl,
    /// <summary>True = Re-open request; False = Re-attempt (technical issue) request.</summary>
    bool IsReopenRequest,
    string Status,
    int AttemptsMade,
    int MaxAttempts,
    DateTime RequestedAt,
    DateTime? ReviewedAt,
    string? ReviewedByName,
    string? RejectionReason,
    /// <summary>Set when a Re-open request is approved. The specific student's deadline.</summary>
    DateTime? GrantedExtensionUntil
);

/// <summary>Student-facing view of their own re-attempt request.</summary>
public record StudentReattemptStatusResponse(
    Guid Id,
    Guid ExamId,
    string ExamTitle,
    string Justification,
    string? AttachmentUrl,
    bool IsReopenRequest,
    string Status,
    int AttemptsMade,
    int MaxAttempts,
    DateTime RequestedAt,
    DateTime? ReviewedAt,
    string? RejectionReason,
    /// <summary>If approved Re-open, this is when the student's access window closes.</summary>
    DateTime? GrantedExtensionUntil
);
