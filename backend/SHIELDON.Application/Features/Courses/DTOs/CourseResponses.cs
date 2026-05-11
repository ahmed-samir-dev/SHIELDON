namespace SHIELDON.Application.Features.Courses.DTOs;

// ── Course Responses ───────────────────────────────────────────────────────

/// <summary>
/// Summary of a course returned in list views.
/// Contains enough info for a card or table row — no deep associations.
/// </summary>
public record CourseResponse(
    Guid Id,
    string Title,
    string CourseCode,
    string? Description,
    Guid? AssignedTutorId,
    string? AssignedTutorName,
    bool IsActive,
    int EnrolledStudentCount,
    DateTime CreatedAt
);

/// <summary>
/// Full course detail returned for a course detail page.
/// Includes enrollment list and material count.
/// </summary>
public record CourseDetailResponse(
    Guid Id,
    string Title,
    string CourseCode,
    string? Description,
    Guid? AssignedTutorId,
    string? AssignedTutorName,
    bool IsActive,
    int EnrolledStudentCount,
    int MaterialCount,
    int AnnouncementCount,
    int AssignmentCount,
    int ExamCount,
    int PublishedExamCount,
    DateTime CreatedAt
);

// ── Enrollment Responses ───────────────────────────────────────────────────

/// <summary>
/// Represents a single enrollment record. Used by Admin/Tutor in management views.
/// </summary>
public record EnrollmentResponse(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string CourseCode,
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    string? StudentDisplayId,
    string Status,
    int RejectionCount,
    DateTime? CooldownUntil,
    string? RejectionReason,
    DateTime RequestedAt,
    DateTime? ReviewedAt,
    string? ReviewedByName
);

/// <summary>
/// Student-facing view of their own enrollment status for a course.
/// Simplified — hides reviewer details.
/// </summary>
public record StudentEnrollmentStatusResponse(
    Guid CourseId,
    string CourseTitle,
    string Status,
    int RejectionCount,
    DateTime? CooldownUntil,
    string? RejectionReason,
    DateTime RequestedAt
);
