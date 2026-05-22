using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Exams.DTOs;
using SHIELDON.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Manages the re-attempt request lifecycle.
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

    // ── POST /api/reattempt-requests ──────────────────────────────────────

    /// <summary>
    /// Student submits a re-attempt request for an exam they have exhausted all attempts on.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<StudentReattemptStatusResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SubmitRequest(
        [FromQuery] Guid examId,
        [FromBody] SubmitReattemptRequest request,
        CancellationToken ct = default)
    {
        var result = await _reattemptService.SubmitRequestAsync(examId, GetUserId(), request, ct);
        return Created(
            $"/api/reattempt-requests/{result.Id}",
            ApiResponse<StudentReattemptStatusResponse>.Ok(result, "Re-attempt request submitted successfully. An admin will review it shortly."));
    }

    // ── GET /api/reattempt-requests ───────────────────────────────────────

    /// <summary>
    /// Returns a paginated list of re-attempt requests.
    /// Admin: all requests. Tutor: their courses. Student: their own only.
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
        return Ok(ApiResponse<PagedResponse<ReattemptRequestResponse>>.Ok(result, "Re-attempt requests retrieved successfully."));
    }

    // ── GET /api/reattempt-requests/mine ─────────────────────────────────

    /// <summary>
    /// Student: Returns all re-attempt requests submitted by the currently authenticated student.
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StudentReattemptStatusResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyRequests(CancellationToken ct = default)
    {
        var result = await _reattemptService.GetMyRequestsAsync(GetUserId(), ct);
        return Ok(ApiResponse<IReadOnlyList<StudentReattemptStatusResponse>>.Ok(result, "Your re-attempt requests retrieved successfully."));
    }

    // ── PATCH /api/reattempt-requests/{requestId}/review ─────────────────

    /// <summary>
    /// Admin/Tutor reviews a pending re-attempt request (approve or reject).
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
            ? "Re-attempt request approved. The student has been notified."
            : "Re-attempt request rejected. The student has been notified.";
        return Ok(ApiResponse<ReattemptRequestResponse>.Ok(result, message));
    }
}
