using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Exams.DTOs;
using SHIELDON.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Manages the request lifecycle for exam re-attempts and exam re-opens.
///
/// Access control:
///   - Admin: full access - can list all requests and review any
///   - Tutor: can list and review requests for their assigned courses
///   - Student: can submit requests and view their own
/// </summary>
[ApiController]
[Route("api/reattempt-requests")]
[Authorize]
public class ReattemptController : ControllerBase
{
    private readonly IReattemptService _reattemptService;

    public ReattemptController(IReattemptService reattemptService)
    {
        _reattemptService = reattemptService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

    // ── POST /api/reattempt-requests ──────────────────────────────────────────

    /// <summary>
    /// Student submits a re-attempt or re-open request for an exam.
    /// Accepts multipart/form-data (Justification, IsReopenRequest, optional AttachmentFile).
    /// Max attachment size: 10 MB. Allowed: .jpg, .jpeg, .png, .pdf, .docx
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Student")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<StudentReattemptStatusResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SubmitRequest(
        [FromQuery] Guid examId,
        [FromForm] string justification,
        [FromForm] bool isReopenRequest = false,
        IFormFile? attachmentFile = null,
        CancellationToken ct = default)
    {
        var request = new SubmitReattemptRequest(justification, isReopenRequest);

        Stream? stream = null;
        string? fileName = null;
        long fileSize = 0;

        if (attachmentFile is not null && attachmentFile.Length > 0)
        {
            stream = attachmentFile.OpenReadStream();
            fileName = attachmentFile.FileName;
            fileSize = attachmentFile.Length;
        }

        await using (stream)
        {
            var result = await _reattemptService.SubmitRequestAsync(
                examId, GetUserId(), request, stream, fileName, fileSize, ct);

            var submittedMessage = isReopenRequest
                ? "Re-open request submitted successfully. A tutor will review it shortly."
                : "Re-attempt request submitted successfully. A tutor will review it shortly.";

            return Created(
                $"/api/reattempt-requests/{result.Id}",
                ApiResponse<StudentReattemptStatusResponse>.Ok(result, submittedMessage));
        }
    }

    // ── GET /api/reattempt-requests/can-reopen ────────────────────────────────

    /// <summary>
    /// Student checks whether they are eligible to submit a Re-open Request for a specific exam.
    /// Returns true if: exam is expired, student has 0 attempts, and no pending request exists.
    /// </summary>
    [HttpGet("can-reopen")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CanReopenRequest(
        [FromQuery] Guid examId,
        CancellationToken ct = default)
    {
        var result = await _reattemptService.CanStudentSubmitReopenRequestAsync(examId, GetUserId(), ct);
        return Ok(ApiResponse<bool>.Ok(result, result
            ? "You are eligible to request a re-open for this exam."
            : "You are not eligible to request a re-open for this exam."));
    }

    // ── GET /api/reattempt-requests ───────────────────────────────────────────

    /// <summary>
    /// Returns a paginated list of requests.
    /// Admin: all. Tutor: their courses. Student: their own only.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ReattemptRequestResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] Guid? examId = null,
        [FromQuery] Guid? courseId = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var query = new ReattemptQueryParams(page, pageSize, status, examId, courseId, searchTerm);
        var result = await _reattemptService.GetRequestsAsync(query, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<PagedResponse<ReattemptRequestResponse>>.Ok(result, "Requests retrieved successfully."));
    }

    // ── GET /api/reattempt-requests/mine ─────────────────────────────────────

    /// <summary>
    /// Student: Returns all requests submitted by the currently authenticated student.
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StudentReattemptStatusResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyRequests(CancellationToken ct = default)
    {
        var result = await _reattemptService.GetMyRequestsAsync(GetUserId(), ct);
        return Ok(ApiResponse<IReadOnlyList<StudentReattemptStatusResponse>>.Ok(result, "Your requests retrieved successfully."));
    }

    // ── PATCH /api/reattempt-requests/{requestId}/review ─────────────────────

    /// <summary>
    /// Admin/Tutor reviews a pending request (approve or reject).
    /// If approving a Re-open Request, set ExtensionHours to 24 or 48.
    /// </summary>
    [HttpPatch("{requestId:guid}/review")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<ReattemptRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewRequest(
        Guid requestId,
        [FromBody] ReviewReattemptRequest request,
        CancellationToken ct = default)
    {
        var result = await _reattemptService.ReviewRequestAsync(requestId, GetUserId(), GetUserRole(), request, ct);
        var message = result.Status == "Approved"
            ? "Request approved. The student has been notified."
            : "Request rejected. The student has been notified.";
        return Ok(ApiResponse<ReattemptRequestResponse>.Ok(result, message));
    }
}
