using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Violations.DTOs;
using SHIELDON.Application.Interfaces;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Anti-Cheating Engine violation persistence endpoints.
///
/// Student: POST /api/violations/batch — sends violation batches during/after exam.
/// Tutor/Admin: GET endpoints to inspect violations per attempt or per exam (Phase 5 UI).
///
/// All endpoints require JWT authentication.
/// </summary>
[ApiController]
[Authorize]
public class ViolationsController : ControllerBase
{
    private readonly IViolationService _violationService;

    public ViolationsController(IViolationService violationService)
    {
        _violationService = violationService;
    }

    private Guid   GetUserId()   => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

    // ── Student: Log Violations Batch ─────────────────────────────────────────

    /// <summary>
    /// POST /api/violations/batch
    ///
    /// Called by the student's Anti-Cheat Engine to persist detected violations.
    /// The engine sends batches every 60 seconds and one final batch on exam submit.
    ///
    /// Only the Student role may call this endpoint.
    /// Each violation's attemptId must belong to the calling student.
    /// </summary>
    [HttpPost("api/violations/batch")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LogViolationBatch(
        [FromBody] BatchViolationRequest request,
        CancellationToken ct)
    {
        var result = await _violationService.LogViolationBatchAsync(request, GetUserId(), ct);
        return Ok(result);
    }

    // ── Tutor/Admin: Get Violations for One Attempt ───────────────────────────

    /// <summary>
    /// GET /api/attempts/{attemptId}/violations
    ///
    /// Returns all violations logged for a specific exam attempt, ordered by time.
    /// Also returns a computed strike score and counts per severity.
    /// Tutor (own courses) and Admin only.
    /// </summary>
    [HttpGet("api/attempts/{attemptId:guid}/violations")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<AttemptViolationSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetViolationsForAttempt(
        Guid attemptId,
        CancellationToken ct)
    {
        var result = await _violationService.GetViolationsForAttemptAsync(
            attemptId, GetUserId(), GetUserRole(), ct);
        return Ok(result);
    }

    // ── Tutor/Admin: Get Violation Summary for Exam ───────────────────────────

    /// <summary>
    /// GET /api/exams/{examId}/violations
    ///
    /// Returns per-attempt violation summaries for all students who took a specific exam.
    /// Results are sorted by StrikeScore descending so the most suspicious attempts appear first.
    /// Tutor (own courses) and Admin only.
    /// </summary>
    [HttpGet("api/exams/{examId:guid}/violations")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<List<AttemptViolationSummary>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetViolationSummaryForExam(
        Guid examId,
        CancellationToken ct)
    {
        var result = await _violationService.GetViolationSummaryForExamAsync(
            examId, GetUserId(), GetUserRole(), ct);
        return Ok(result);
    }
}
