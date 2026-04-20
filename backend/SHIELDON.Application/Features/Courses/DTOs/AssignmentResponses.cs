namespace SHIELDON.Application.Features.Courses.DTOs;

// ── Assignment Responses ───────────────────────────────────────────────────

/// <summary>
/// Full details of a course assignment as returned to any authenticated caller.
/// For Student role: MySubmission contains the student's own submission if one exists, otherwise null.
/// For Tutor/Admin role: MySubmission is always null; SubmissionCount shows total student submissions.
/// </summary>
public record AssignmentResponse(
    Guid Id,
    Guid CourseId,
    string Title,
    string? Instructions,
    string CreatedByName,
    // ── Reference File ──
    bool HasReferenceFile,
    string? ReferenceFileName,
    string? ReferenceFileExtension,
    long? ReferenceFileSizeBytes,
    // ── Deadline ──
    DateTime? DueDate,
    bool IsPastDue,
    // ── Submission Info ──
    int SubmissionCount,                              // For Tutor/Admin: total. For Student: 0 or 1.
    AssignmentSubmissionResponse? MySubmission,       // Non-null only for Student role
    // ── Metadata ──
    DateTime CreatedAt
);

/// <summary>
/// A single student's submission for an assignment.
/// Returned as part of AssignmentResponse.MySubmission (for the requesting student)
/// or in the full submission list endpoint (for Tutor/Admin).
/// </summary>
public record AssignmentSubmissionResponse(
    Guid Id,
    Guid AssignmentId,
    Guid StudentId,
    string StudentName,
    string? StudentDisplayId,
    string OriginalFileName,
    string FileExtension,
    long FileSizeBytes,
    DateTime SubmittedAt
);
