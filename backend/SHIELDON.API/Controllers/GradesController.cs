using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Grades.DTOs;
using SHIELDON.Application.Interfaces;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Grade Management Panel endpoints.
///
/// Tutor/Admin: view course grades, update weight/score/notes, publish, export CSV.
/// Student: view own published grades (all courses or per course).
///
/// All endpoints require JWT authentication.
/// </summary>
[ApiController]
[Authorize]
public class GradesController : ControllerBase
{
    private readonly IGradeService _gradeService;

    public GradesController(IGradeService gradeService)
    {
        _gradeService = gradeService;
    }

    private Guid   GetUserId()   => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

    // ── Tutor/Admin: Course Grade Table ───────────────────────────────────────

    /// <summary>
    /// GET /api/courses/{courseId}/grades
    /// Returns a paginated per-student grade summary for a course.
    /// Includes all grade items (exams + assignments), weights, and final score.
    /// Admin or assigned Tutor only.
    /// </summary>
    [HttpGet("api/courses/{courseId:guid}/grades")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CourseGradeSummaryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourseGrades(
        Guid courseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var query = new GradeQueryParams(page, pageSize, type, status, searchTerm);
        var result = await _gradeService.GetCourseGradesAsync(courseId, query, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<PagedResponse<CourseGradeSummaryResponse>>.Ok(result, "Course grades retrieved successfully."));
    }

    // ── Student: My Grades in a specific course ────────────────────────────────

    /// <summary>
    /// GET /api/courses/{courseId}/grades/my
    /// Returns the student's own published grades for a specific course.
    /// Student role only.
    /// </summary>
    [HttpGet("api/courses/{courseId:guid}/grades/my")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MyGradeItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyGradesForCourse(
        Guid courseId,
        CancellationToken ct = default)
    {
        var result = await _gradeService.GetMyGradesForCourseAsync(courseId, GetUserId(), ct);
        return Ok(ApiResponse<IReadOnlyList<MyGradeItemResponse>>.Ok(result, "Your grades retrieved successfully."));
    }

    // ── Student: My Grades across all courses ─────────────────────────────────

    /// <summary>
    /// GET /api/my-grades
    /// Returns all published grades for the requesting student across all enrolled courses.
    /// Student role only.
    /// </summary>
    [HttpGet("api/my-grades")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MyGradeItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyGrades(CancellationToken ct = default)
    {
        var result = await _gradeService.GetMyGradesAsync(GetUserId(), ct);
        return Ok(ApiResponse<IReadOnlyList<MyGradeItemResponse>>.Ok(result, "Your grades retrieved successfully."));
    }

    // ── Tutor/Admin: Update Grade (weight / score override / notes) ────────────

    /// <summary>
    /// PATCH /api/grades/{gradeId}
    /// Updates weight, score override, or notes on a grade record.
    /// Weight change propagates to ALL students for the same exam/assignment.
    /// Admin or assigned Tutor only.
    /// </summary>
    [HttpPatch("api/grades/{gradeId:guid}")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<GradeItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGrade(
        Guid gradeId,
        [FromBody] UpdateGradeRequest request,
        CancellationToken ct = default)
    {
        var result = await _gradeService.UpdateGradeAsync(gradeId, request, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<GradeItemResponse>.Ok(result, "Grade updated successfully."));
    }

    // ── Tutor/Admin: Bulk Publish ─────────────────────────────────────────────

    /// <summary>
    /// POST /api/courses/{courseId}/grades/publish
    /// Publishes specific grade records (by ID) or all unpublished records in a course.
    /// Admin or assigned Tutor only.
    /// </summary>
    [HttpPost("api/courses/{courseId:guid}/grades/publish")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishGrades(
        Guid courseId,
        [FromBody] BulkPublishRequest request,
        CancellationToken ct = default)
    {
        var message = await _gradeService.PublishGradesAsync(courseId, request, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<string>.Ok(message));
    }

    // ── Tutor/Admin: CSV Export ───────────────────────────────────────────────

    /// <summary>
    /// GET /api/courses/{courseId}/grades/export
    /// Streams a CSV file with all grade records for a course.
    /// Admin or assigned Tutor only.
    /// </summary>
    [HttpGet("api/courses/{courseId:guid}/grades/export")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportGradesCsv(
        Guid courseId,
        CancellationToken ct = default)
    {
        var (csvBytes, fileName) = await _gradeService.ExportGradesCsvAsync(courseId, GetUserId(), GetUserRole(), ct);
        return File(csvBytes, "text/csv", fileName);
    }
}
