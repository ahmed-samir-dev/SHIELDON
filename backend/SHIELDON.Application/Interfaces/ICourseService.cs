using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Courses.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Course management service contract.
/// Handles CRUD operations for courses and the full enrollment request workflow.
/// </summary>
public interface ICourseService
{
    // ── Course CRUD (Admin only) ──────────────────────────────────────────

    /// <summary>Creates a new course. CourseCode must be unique.</summary>
    Task<CourseResponse> CreateCourseAsync(Guid adminId, CreateCourseRequest request, CancellationToken ct = default);

    /// <summary>Returns a paginated list of courses. Filtered by IsActive and search term if provided.</summary>
    Task<PagedResponse<CourseResponse>> GetCoursesAsync(CourseQueryParams query, Guid requestingUserId, string requestingUserRole, CancellationToken ct = default);

    /// <summary>Returns the full detail of a single course by ID.</summary>
    Task<CourseDetailResponse> GetCourseByIdAsync(Guid courseId, CancellationToken ct = default);

    /// <summary>Updates course fields (title, description, assigned tutor, active status). Admin only.</summary>
    Task<CourseResponse> UpdateCourseAsync(Guid courseId, UpdateCourseRequest request, Guid requestingUserId, string requestingUserRole, CancellationToken ct = default);

    /// <summary>Deletes a course. Admin only. Cascades to enrollments, materials, announcements.</summary>
    Task DeleteCourseAsync(Guid courseId, CancellationToken ct = default);

    // ── Enrollment Workflow ───────────────────────────────────────────────

    /// <summary>
    /// Student requests enrollment in a course.
    /// Enforces: no duplicate pending requests, cooldown after 2 consecutive rejections,
    /// permanent block after 3 total rejections.
    /// </summary>
    Task<StudentEnrollmentStatusResponse> RequestEnrollmentAsync(Guid studentId, EnrollmentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Returns paginated pending enrollment requests.
    /// Admin: all courses. Tutor: only their assigned courses.
    /// </summary>
    Task<PagedResponse<EnrollmentResponse>> GetPendingEnrollmentsAsync(Guid reviewerId, string reviewerRole, EnrollmentQueryParams query, CancellationToken ct = default);

    /// <summary>
    /// Returns approved enrollment records (enrolled students data).
    /// Admin: all courses. Tutor: only their assigned courses.
    /// </summary>
    Task<PagedResponse<EnrollmentResponse>> GetApprovedEnrollmentsAsync(Guid reviewerId, string reviewerRole, EnrollmentQueryParams query, CancellationToken ct = default);

    /// <summary>
    /// Admin/Tutor approves or rejects a single enrollment request.
    /// On approval: status → Approved, student gets notification.
    /// On rejection: RejectionCount++, applies 24h cooldown if ≥ 2 consecutive rejections.
    /// </summary>
    Task<EnrollmentResponse> ReviewEnrollmentAsync(Guid enrollmentId, Guid reviewerId, ReviewEnrollmentRequest request, CancellationToken ct = default);

    /// <summary>Approve or reject multiple enrollment requests at once.</summary>
    Task<int> BulkReviewEnrollmentsAsync(BulkReviewEnrollmentRequest request, Guid reviewerId, CancellationToken ct = default);

    /// <summary>Returns the requesting student's enrollment status for all courses they've interacted with, with pagination and filtering.</summary>
    Task<PagedResponse<StudentEnrollmentStatusResponse>> GetMyEnrollmentsAsync(Guid studentId, StudentEnrollmentQueryParams query, CancellationToken ct = default);
}
