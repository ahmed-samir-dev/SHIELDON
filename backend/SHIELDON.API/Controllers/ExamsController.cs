using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Exams.DTOs;
using SHIELDON.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Manages the exam lifecycle for courses.
///
/// Access control:
///   - Admin: full access to all endpoints
///   - Tutor: can manage exams only for courses assigned to them
///   - Student: read-only access to Published exams in enrolled courses
/// </summary>
[ApiController]
[Route("api/courses/{courseId:guid}/exams")]
[Authorize]
public class ExamsController : ControllerBase
{
    private readonly IExamService _examService;

    public ExamsController(IExamService examService)
    {
        _examService = examService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

    // ── GET /api/courses/{courseId}/exams ─────────────────────────────────

    /// <summary>
    /// Returns a paginated list of exams for a course.
    /// Tutors/Admins see all statuses; Students only see Published exams.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ExamSummaryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExams(
        Guid courseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var query = new ExamQueryParams(page, pageSize, search, status);
        var result = await _examService.GetExamsAsync(courseId, query, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<PagedResponse<ExamSummaryResponse>>.Ok(result, "Exams retrieved successfully."));
    }

    // ── POST /api/courses/{courseId}/exams ────────────────────────────────

    /// <summary>Tutor/Admin creates a new draft exam for a course.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<ExamSummaryResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateExam(
        Guid courseId,
        [FromBody] CreateExamRequest request,
        CancellationToken ct = default)
    {
        var result = await _examService.CreateExamAsync(courseId, request, GetUserId(), GetUserRole(), ct);
        return Created($"/api/exams/{result.Id}", ApiResponse<ExamSummaryResponse>.Ok(result, "Exam created successfully."));
    }
}

/// <summary>
/// Exam-level endpoints that operate on a specific exam by its ID.
/// Separated from course-scoped routing for cleaner URL structure.
/// </summary>
[ApiController]
[Route("api/exams")]
[Authorize]
public class ExamActionsController : ControllerBase
{
    private readonly IExamService _examService;

    public ExamActionsController(IExamService examService)
    {
        _examService = examService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

    // ── GET /api/exams/{examId} ───────────────────────────────────────────

    /// <summary>Returns full detail of a single exam.</summary>
    [HttpGet("{examId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ExamDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExamById(Guid examId, CancellationToken ct = default)
    {
        var result = await _examService.GetExamByIdAsync(examId, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<ExamDetailResponse>.Ok(result, "Exam retrieved successfully."));
    }

    // ── PATCH /api/exams/{examId} ─────────────────────────────────────────

    /// <summary>Tutor/Admin updates a draft or published exam.</summary>
    [HttpPatch("{examId:guid}")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<ExamDetailResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateExam(
        Guid examId,
        [FromBody] UpdateExamRequest request,
        CancellationToken ct = default)
    {
        var result = await _examService.UpdateExamAsync(examId, request, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<ExamDetailResponse>.Ok(result, "Exam updated successfully."));
    }

    // ── DELETE /api/exams/{examId} ────────────────────────────────────────

    /// <summary>Deletes a Draft exam. Cannot delete Published or Closed exams.</summary>
    [HttpDelete("{examId:guid}")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteExam(Guid examId, CancellationToken ct = default)
    {
        await _examService.DeleteExamAsync(examId, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<object>.Ok("Exam deleted successfully."));
    }

    // ── PATCH /api/exams/{examId}/publish ─────────────────────────────────

    /// <summary>
    /// Publishes an exam (Draft → Published).
    /// Triggers in-app + email notification to all enrolled students.
    /// </summary>
    [HttpPatch("{examId:guid}/publish")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<ExamDetailResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PublishExam(Guid examId, CancellationToken ct = default)
    {
        var result = await _examService.PublishExamAsync(examId, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<ExamDetailResponse>.Ok(result, "Exam published successfully. Students have been notified."));
    }
}
