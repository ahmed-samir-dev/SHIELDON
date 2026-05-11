namespace SHIELDON.Application.Features.Courses.DTOs;

// ── Course Requests ────────────────────────────────────────────────────────

/// <summary>Request body to create a new course. Admin only.</summary>
public record CreateCourseRequest(
    string Title,
    string CourseCode,
    string? Description,
    Guid? AssignedTutorId
);

/// <summary>Request body to update an existing course. Admin only.</summary>
public record UpdateCourseRequest(
    string Title,
    string? Description,
    Guid? AssignedTutorId,
    bool IsActive
);

// ── Enrollment Requests ────────────────────────────────────────────────────

/// <summary>Student submits this to request enrollment in a course.</summary>
public record EnrollmentRequest(Guid CourseId);

/// <summary>Admin/Tutor reviews a pending enrollment request.</summary>
public record ReviewEnrollmentRequest(
    bool Approved,
    string? RejectionReason
);

/// <summary>Admin/Tutor reviews multiple enrollment requests at once.</summary>
public record BulkReviewEnrollmentRequest(
    IList<Guid> EnrollmentIds,
    bool Approved,
    string? RejectionReason
);

// ── Query Params ───────────────────────────────────────────────────────────

/// <summary>Query parameters for paginated course listing.</summary>
public record CourseQueryParams(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null,
    string? EnrollmentStatus = null
);

/// <summary>Query parameters for paginated enrollment listing.</summary>
public record EnrollmentQueryParams(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    Guid? CourseId = null
);
