using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Courses.DTOs;
using SHIELDON.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Manages course CRUD operations and enrollment request workflow.
/// - Admin: full access to all endpoints
/// - Tutor: read-only on courses, manage enrollments for assigned courses
/// - Student: browse courses, manage own enrollment requests
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

    // ── Course CRUD ──────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/courses?page=1&pageSize=10&search=cs&isActive=true
    /// Returns paginated courses. Admins see all, Tutors see assigned only, Students see active only.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CourseResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? enrollmentStatus = null,
        CancellationToken cancellationToken = default)
    {
        var query = new CourseQueryParams(page, pageSize, search, isActive, enrollmentStatus);
        var result = await _courseService.GetCoursesAsync(query, GetUserId(), GetUserRole(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<CourseResponse>>.Ok(result, "Courses retrieved successfully."));
    }

    /// <summary>
    /// GET /api/courses/{id}
    /// Returns full course detail. Accessible by all authenticated roles.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CourseDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourse(Guid id, CancellationToken cancellationToken)
    {
        var course = await _courseService.GetCourseByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<CourseDetailResponse>.Ok(course, "Course retrieved successfully."));
    }

    /// <summary>
    /// POST /api/courses
    /// Creates a new course. Admin role required.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<CourseResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCourse(
        [FromBody] CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var course = await _courseService.CreateCourseAsync(GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetCourse), new { id = course.Id },
            ApiResponse<CourseResponse>.Ok(course, "Course created successfully."));
    }

    /// <summary>
    /// PATCH /api/courses/{id}
    /// Updates a course (title, description, assigned tutor, active flag). Admin or assigned Tutor.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<CourseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCourse(
        Guid id,
        [FromBody] UpdateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var course = await _courseService.UpdateCourseAsync(id, request, GetUserId(), GetUserRole(), cancellationToken);
        return Ok(ApiResponse<CourseResponse>.Ok(course, "Course updated successfully."));
    }

    /// <summary>
    /// DELETE /api/courses/{id}
    /// Deletes a course and all its content. Admin role required.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCourse(Guid id, CancellationToken cancellationToken)
    {
        await _courseService.DeleteCourseAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok("Course deleted successfully."));
    }

    // ── Enrollment ───────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/courses/{id}/enroll
    /// Student requests enrollment in a course. Enforces cooldown and rejection limits.
    /// </summary>
    [HttpPost("{id:guid}/enroll")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<StudentEnrollmentStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestEnrollment(Guid id, CancellationToken cancellationToken)
    {
        var result = await _courseService.RequestEnrollmentAsync(
            GetUserId(), new EnrollmentRequest(id), cancellationToken);

        return Ok(ApiResponse<StudentEnrollmentStatusResponse>.Ok(result, "Enrollment request submitted successfully."));
    }

    /// <summary>
    /// GET /api/courses/enrollments/pending?page=1&pageSize=10&search=...&courseId=guid
    /// Returns pending enrollment requests (paginated). Admin sees all; Tutor sees their courses only.
    /// </summary>
    [HttpGet("enrollments/pending")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<EnrollmentResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingEnrollments(
        [FromQuery] EnrollmentQueryParams query,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.GetPendingEnrollmentsAsync(
            GetUserId(), GetUserRole(), query, cancellationToken);

        return Ok(ApiResponse<PagedResponse<EnrollmentResponse>>.Ok(result, "Pending enrollments retrieved successfully."));
    }

    /// <summary>
    /// GET /api/courses/enrollments/approved?courseId=guid&page=1&pageSize=10&search=...
    /// Returns approved enrollment records (enrolled students). Admin sees all; Tutor sees their courses only.
    /// </summary>
    [HttpGet("enrollments/approved")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<EnrollmentResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApprovedEnrollments(
        [FromQuery] EnrollmentQueryParams query,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.GetApprovedEnrollmentsAsync(
            GetUserId(), GetUserRole(), query, cancellationToken);

        return Ok(ApiResponse<PagedResponse<EnrollmentResponse>>.Ok(result, "Approved enrollments retrieved successfully."));
    }

    /// <summary>
    /// PATCH /api/courses/enrollments/{enrollmentId}/review
    /// Approve or reject a single pending enrollment. Admin or Tutor only.
    /// </summary>
    [HttpPatch("enrollments/{enrollmentId:guid}/review")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReviewEnrollment(
        Guid enrollmentId,
        [FromBody] ReviewEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.ReviewEnrollmentAsync(
            enrollmentId, GetUserId(), request, cancellationToken);

        var action = request.Approved ? "approved" : "rejected";
        return Ok(ApiResponse<EnrollmentResponse>.Ok(result, $"Enrollment {action} successfully."));
    }

    /// <summary>
    /// POST /api/courses/enrollments/bulk-review
    /// Approve or reject multiple enrollment requests at once. Admin or Tutor only.
    /// </summary>
    [HttpPost("enrollments/bulk-review")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkReviewEnrollments(
        [FromBody] BulkReviewEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        var count = await _courseService.BulkReviewEnrollmentsAsync(request, GetUserId(), cancellationToken);
        var action = request.Approved ? "approved" : "rejected";
        return Ok(ApiResponse<object>.Ok($"{count} enrollment(s) {action} successfully."));
    }

    /// <summary>
    /// GET /api/courses/enrollments/my?page=1&pageSize=10&searchTerm=cs&status=Pending&requestedFrom=2025-01-01&requestedTo=2025-12-31
    /// Returns the authenticated student's enrollment records (paginated, with filters).
    /// </summary>
    [HttpGet("enrollments/my")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<StudentEnrollmentStatusResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEnrollments(
        [FromQuery] StudentEnrollmentQueryParams query,
        CancellationToken cancellationToken)
    {
        var result = await _courseService.GetMyEnrollmentsAsync(GetUserId(), query, cancellationToken);
        return Ok(ApiResponse<PagedResponse<StudentEnrollmentStatusResponse>>.Ok(result, "Your enrollments retrieved successfully."));
    }
}
