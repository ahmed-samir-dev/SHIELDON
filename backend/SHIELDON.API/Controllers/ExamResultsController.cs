using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Exams.DTOs;
using SHIELDON.Application.Interfaces;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Handles exam result retrieval, short-answer manual grading, and result publication.
/// Routes:
///   GET  /api/exam-attempts/{attemptId}/result  - Student/Tutor/Admin get result
///   GET  /api/exams/{examId}/attempts            - Tutor/Admin get all attempts
///   POST /api/exam-attempts/{attemptId}/grade-short-answers - Tutor/Admin grade short answers
///   POST /api/exams/{examId}/release-results     - Tutor/Admin release results
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class ExamResultsController : ControllerBase
{
    private readonly IExamResultService _resultService;

    public ExamResultsController(IExamResultService resultService)
    {
        _resultService = resultService;
    }

    // ── GET result for a single attempt ─────────────────────────────────────

    /// <summary>
    /// Returns the full result for a specific attempt.
    /// Students see their own; Tutors see attempts in their courses; Admins see all.
    /// Result visibility (score + per-question review) is governed by ResultVisibility setting.
    /// </summary>
    [HttpGet("exam-attempts/{attemptId}/result")]
    [ProducesResponseType(typeof(ApiResponse<ExamResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAttemptResult(Guid attemptId, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;

        var result = await _resultService.GetAttemptResultAsync(attemptId, userId, role, ct);
        return Ok(result);
    }

    // ── GET student's own attempts for an exam ──────────────────────────────

    /// <summary>
    /// Returns all attempts for a given exam made by the requesting student.
    /// Used for "My Results" frontend view.
    /// </summary>
    [HttpGet("exams/{examId}/my-attempts")]
    [Authorize(Policy = "RequireStudent")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ExamAttemptSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAttempts(Guid examId, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _resultService.GetStudentAttemptsAsync(examId, userId, ct);
        return Ok(result);
    }

    // ── GET all attempts for an exam (Tutor / Admin panel) ──────────────────

    /// <summary>
    /// Returns all completed attempts for a given exam.
    /// Used by Tutors and Admins to see the results table.
    /// </summary>
    [HttpGet("exams/{examId}/attempts")]
    [Authorize(Policy = "RequireTutorOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ExamAttemptSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExamAttempts(Guid examId, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;

        var result = await _resultService.GetExamAttemptsAsync(examId, userId, role, ct);
        return Ok(result);
    }

    // ── POST: Tutor grades short answers ────────────────────────────────────

    /// <summary>
    /// Assigns points to short-answer questions for a Submitted attempt.
    /// Finalises grading (status → Graded) and creates/updates GradeRecord.
    /// </summary>
    [HttpPost("exam-attempts/{attemptId}/grade-short-answers")]
    [Authorize(Policy = "RequireTutorOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GradeShortAnswers(
        Guid attemptId,
        [FromBody] GradeShortAnswerRequest request,
        CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;

        var result = await _resultService.GradeShortAnswersAsync(attemptId, request, userId, role, ct);
        return Ok(result);
    }

    // ── POST: Tutor / Admin release results ─────────────────────────────────

    /// <summary>
    /// Publishes grade records for all (or specified) students on a ManualRelease exam.
    /// Sends a result-released notification + email to each affected student.
    /// </summary>
    [HttpPost("exams/{examId}/release-results")]
    [Authorize(Policy = "RequireTutorOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReleaseResults(
        Guid examId,
        [FromBody] ReleaseResultsRequest request,
        CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;

        var result = await _resultService.ReleaseResultsAsync(examId, request, userId, role, ct);
        return Ok(result);
    }

    // ── GET: Tutor / Admin export results to CSV ────────────────────────────

    /// <summary>
    /// Exports all attempts for a given exam as a CSV file.
    /// </summary>
    [HttpGet("exams/{examId}/export")]
    [Authorize(Policy = "RequireTutorOrAdmin")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportResults(Guid examId, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;

        var csvBytes = await _resultService.ExportResultsToCsvAsync(examId, userId, role, ct);
        return File(csvBytes, "text/csv", $"exam_{examId}_results.csv");
    }
}
