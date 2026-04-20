using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Courses.DTOs;
using SHIELDON.Application.Interfaces;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Manages the full assignment lifecycle for a course.
///
/// Assignment CRUD  → Tutor (assigned) / Admin only
/// Reference file   → enrolled Students + Tutor/Admin
/// Submissions      → Student submits; Tutor/Admin views all + bulk ZIP download
///
/// All endpoints require JWT authentication.
/// </summary>
[ApiController]
[Route("api/courses/{courseId:guid}/assignments")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;

    public AssignmentsController(IAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    private Guid   GetUserId()   => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

    // ════════════════════════════════════════════════════════════════════════
    // ASSIGNMENT CRUD
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// POST /api/courses/{courseId}/assignments
    /// Creates a new assignment in the course.
    /// An optional reference file (PDF, Word, PowerPoint, Excel, image, or ZIP; max 50 MB)
    /// may be attached via multipart/form-data.
    /// Admin or assigned Tutor only.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Tutor")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<AssignmentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateAssignment(
        Guid courseId,
        [FromForm] CreateAssignmentRequest request,
        IFormFile? referenceFile,
        CancellationToken cancellationToken)
    {
        UploadedFileDto? fileDto = null;
        if (referenceFile is { Length: > 0 })
        {
            fileDto = new UploadedFileDto(
                referenceFile.OpenReadStream(),
                referenceFile.FileName,
                referenceFile.ContentType,
                referenceFile.Length);
        }

        var result = await _assignmentService.CreateAssignmentAsync(
            courseId, request, fileDto, GetUserId(), GetUserRole(), cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<AssignmentResponse>.Ok(result, "Assignment created successfully."));
    }

    /// <summary>
    /// GET /api/courses/{courseId}/assignments
    /// Returns all assignments for the course.
    /// Students: must be Approved-enrolled; MySubmission field is populated.
    /// Tutor/Admin: SubmissionCount is populated.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AssignmentResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssignments(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var result = await _assignmentService.GetAssignmentsAsync(
            courseId, GetUserId(), GetUserRole(), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<AssignmentResponse>>.Ok(result, "Assignments retrieved successfully."));
    }

    /// <summary>
    /// PATCH /api/courses/{courseId}/assignments/{assignmentId}
    /// Updates an assignment's title, instructions, or due date.
    /// Admin or assigned Tutor only.
    /// </summary>
    [HttpPatch("{assignmentId:guid}")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<AssignmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAssignment(
        Guid courseId,
        Guid assignmentId,
        [FromBody] UpdateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assignmentService.UpdateAssignmentAsync(
            assignmentId, request, GetUserId(), GetUserRole(), cancellationToken);

        return Ok(ApiResponse<AssignmentResponse>.Ok(result, "Assignment updated successfully."));
    }

    /// <summary>
    /// DELETE /api/courses/{courseId}/assignments/{assignmentId}
    /// Permanently deletes an assignment, its reference file, and ALL student submissions.
    /// Admin or assigned Tutor only.
    /// </summary>
    [HttpDelete("{assignmentId:guid}")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAssignment(
        Guid courseId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        await _assignmentService.DeleteAssignmentAsync(
            assignmentId, GetUserId(), GetUserRole(), cancellationToken);

        return Ok(ApiResponse<object>.Ok("Assignment and all its submissions deleted successfully."));
    }

    // ════════════════════════════════════════════════════════════════════════
    // REFERENCE FILE
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// GET /api/courses/{courseId}/assignments/{assignmentId}/reference
    /// Downloads the reference file attached to an assignment.
    /// Available to enrolled students, the assigned Tutor, and Admin.
    /// Returns 404 if no reference file is attached.
    /// </summary>
    [HttpGet("{assignmentId:guid}/reference")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadReferenceFile(
        Guid courseId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var (stream, contentType, fileName) = await _assignmentService.DownloadReferenceFileAsync(
            assignmentId, GetUserId(), GetUserRole(), cancellationToken);

        return File(stream, contentType, fileName);
    }

    // ════════════════════════════════════════════════════════════════════════
    // STUDENT SUBMISSIONS
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// POST /api/courses/{courseId}/assignments/{assignmentId}/submissions
    /// Uploads a student's submission file for an assignment.
    /// Student must be Approved-enrolled. Deadline must not have passed.
    /// One submission per student per assignment (409 if already submitted).
    /// Max file size: 100 MB.
    /// Student role only.
    /// </summary>
    [HttpPost("{assignmentId:guid}/submissions")]
    [Authorize(Roles = "Student")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<AssignmentSubmissionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitAssignment(
        Guid courseId,
        Guid assignmentId,
        IFormFile submissionFile,
        CancellationToken cancellationToken)
    {
        if (submissionFile is null || submissionFile.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("A submission file is required."));

        var fileDto = new UploadedFileDto(
            submissionFile.OpenReadStream(),
            submissionFile.FileName,
            submissionFile.ContentType,
            submissionFile.Length);

        var result = await _assignmentService.SubmitAssignmentAsync(
            assignmentId, GetUserId(), fileDto, cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<AssignmentSubmissionResponse>.Ok(result, "Assignment submitted successfully."));
    }

    /// <summary>
    /// GET /api/courses/{courseId}/assignments/{assignmentId}/submissions
    /// Returns all student submissions for a specific assignment.
    /// Tutor (assigned) or Admin only.
    /// </summary>
    [HttpGet("{assignmentId:guid}/submissions")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AssignmentSubmissionResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubmissions(
        Guid courseId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var result = await _assignmentService.GetSubmissionsAsync(
            assignmentId, GetUserId(), GetUserRole(), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<AssignmentSubmissionResponse>>.Ok(result, "Submissions retrieved successfully."));
    }

    /// <summary>
    /// DELETE /api/courses/{courseId}/assignments/{assignmentId}/submissions/{submissionId}
    /// Deletes a submission and its file from disk.
    /// Student: can delete their own only, and only before the assignment deadline.
    /// Tutor (assigned) / Admin: can delete any submission at any time.
    /// </summary>
    [HttpDelete("{assignmentId:guid}/submissions/{submissionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubmission(
        Guid courseId,
        Guid assignmentId,
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        await _assignmentService.DeleteSubmissionAsync(
            submissionId, GetUserId(), GetUserRole(), cancellationToken);

        return Ok(ApiResponse<object>.Ok("Submission deleted successfully."));
    }

    /// <summary>
    /// GET /api/courses/{courseId}/assignments/{assignmentId}/submissions/{submissionId}/download
    /// Downloads a single student submission file.
    /// Student: own submission only. Tutor (assigned) / Admin: any submission.
    /// </summary>
    [HttpGet("{assignmentId:guid}/submissions/{submissionId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadSubmission(
        Guid courseId,
        Guid assignmentId,
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        var (stream, contentType, fileName) = await _assignmentService.DownloadSubmissionAsync(
            submissionId, GetUserId(), GetUserRole(), cancellationToken);

        return File(stream, contentType, fileName);
    }

    /// <summary>
    /// GET /api/courses/{courseId}/assignments/{assignmentId}/submissions/download-all
    /// Packages all student submissions for an assignment into a ZIP archive and streams it.
    /// ZIP structure: {studentId}_{studentName}/{originalFileName}
    /// ZIP filename:  {CourseCode}_{AssignmentTitle}_{yyyy-MM-dd}.zip
    /// Returns 204 No Content if no submissions have been made yet.
    /// Tutor (assigned) or Admin only.
    /// </summary>
    [HttpGet("{assignmentId:guid}/submissions/download-all")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAllSubmissionsAsZip(
        Guid courseId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var (zipStream, zipFileName) = await _assignmentService.DownloadAllSubmissionsAsZipAsync(
            assignmentId, GetUserId(), GetUserRole(), cancellationToken);

        if (zipStream is null)
            return NoContent(); // 204 — no submissions yet

        return File(zipStream, "application/zip", zipFileName);
    }
}
