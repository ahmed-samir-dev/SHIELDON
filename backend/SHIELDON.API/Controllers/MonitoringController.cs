using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Monitoring.DTOs;
using SHIELDON.Application.Interfaces;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Phase 5 — Monitoring & Dashboards endpoints.
///
/// Student:
///   POST /api/attempts/{attemptId}/heartbeat — sends heartbeat every 15 seconds
///
/// Tutor/Admin:
///   GET  /api/attempts/{attemptId}/timeline          — session timeline (merged events)
///   GET  /api/attempts/{attemptId}/violations/summary — violation stats + chart data
///   POST /api/attempts/{attemptId}/review             — submit manual review decision
///   POST /api/attempts/{attemptId}/terminate          — live session termination
///   GET  /api/monitoring/tutor/dashboard              — tutor dashboard data
///   GET  /api/monitoring/admin/dashboard              — admin dashboard data
/// </summary>
[ApiController]
[Authorize]
public class MonitoringController : ControllerBase
{
    private readonly IMonitoringService _monitoring;

    public MonitoringController(IMonitoringService monitoring)
    {
        _monitoring = monitoring;
    }

    private Guid   GetUserId()   => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

    // ── Student: Heartbeat ────────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/attempts/{attemptId}/heartbeat
    ///
    /// Called by the student's browser every 15 seconds to confirm an active exam session.
    /// Updates the attempt's LastHeartbeatAt and inserts a HeartbeatReceived presence event.
    /// Student role only; the attemptId must belong to the calling student.
    /// </summary>
    [HttpPost("api/attempts/{attemptId:guid}/heartbeat")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogHeartbeat(Guid attemptId)
    {
        await _monitoring.LogHeartbeatAsync(attemptId, GetUserId());
        return Ok(ApiResponse<string>.Ok("Heartbeat received."));
    }

    // ── Tutor/Admin: Session Timeline ─────────────────────────────────────────────

    /// <summary>
    /// GET /api/attempts/{attemptId}/timeline
    ///
    /// Returns the full, chronologically merged session timeline for one attempt.
    /// Combines PresenceLogs (lifecycle events) and ViolationLogs (anti-cheat events).
    /// Tutor (own courses) and Admin only.
    /// </summary>
    [HttpGet("api/attempts/{attemptId:guid}/timeline")]
    [Authorize(Roles = "Tutor,Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<TimelineEventResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimeline(Guid attemptId)
    {
        var result = await _monitoring.GetTimelineAsync(attemptId, GetUserId(), GetUserRole());
        return Ok(ApiResponse<List<TimelineEventResponse>>.Ok(result));
    }

    // ── Tutor/Admin: Violation Summary ────────────────────────────────────────────

    /// <summary>
    /// GET /api/attempts/{attemptId}/violations/summary
    ///
    /// Returns aggregate violation statistics (counts per severity) and
    /// ECharts-ready chart data (violations over time by minute).
    /// Tutor (own courses) and Admin only.
    /// </summary>
    [HttpGet("api/attempts/{attemptId:guid}/violations/summary")]
    [Authorize(Roles = "Tutor,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ViolationSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetViolationSummary(Guid attemptId)
    {
        var result = await _monitoring.GetViolationSummaryAsync(attemptId, GetUserId(), GetUserRole());
        return Ok(ApiResponse<ViolationSummaryResponse>.Ok(result));
    }

    // ── Tutor/Admin: Submit Review Decision ───────────────────────────────────────

    /// <summary>
    /// POST /api/attempts/{attemptId}/review
    ///
    /// Submits a manual review decision for a suspicious/force-submitted attempt.
    /// - Accepted: score stands as-is.
    /// - MarkedAsCheating: score zeroed in GradeRecord.
    /// - ReAttemptGranted: creates a new approved ReattemptRequest.
    /// One decision per attempt — will fail if a decision already exists.
    /// Tutor (own courses) and Admin only.
    /// </summary>
    [HttpPost("api/attempts/{attemptId:guid}/review")]
    [Authorize(Roles = "Tutor,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ReviewDecisionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitReviewDecision(
        Guid attemptId,
        [FromBody] ReviewDecisionRequest request)
    {
        var result = await _monitoring.SubmitReviewDecisionAsync(attemptId, request, GetUserId());
        return Ok(ApiResponse<ReviewDecisionResponse>.Ok(result));
    }

    // ── Tutor/Admin: Terminate Session ────────────────────────────────────────────

    /// <summary>
    /// POST /api/attempts/{attemptId}/terminate
    ///
    /// Immediately force-submits an active student exam session.
    /// Creates a TutorTerminated presence log entry.
    /// Only works on InProgress attempts.
    /// Tutor (own courses) and Admin only.
    /// </summary>
    [HttpPost("api/attempts/{attemptId:guid}/terminate")]
    [Authorize(Roles = "Tutor,Admin")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TerminateSession(
        Guid attemptId,
        [FromBody] TerminateSessionRequest request)
    {
        await _monitoring.TerminateSessionAsync(attemptId, GetUserId(), request.Reason);
        return Ok(ApiResponse<string>.Ok("Session terminated successfully. Student exam has been force-submitted."));
    }

    // ── Tutor: Dashboard ──────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/monitoring/tutor/dashboard
    ///
    /// Returns the full tutor dashboard payload:
    /// - Active exams panel (per course)
    /// - Live student status grid (for 10-second polling)
    /// - Violation type distribution (ECharts doughnut data)
    /// Tutor role only.
    /// </summary>
    [HttpGet("api/monitoring/tutor/dashboard")]
    [Authorize(Roles = "Tutor")]
    [ProducesResponseType(typeof(ApiResponse<TutorDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTutorDashboard()
    {
        var result = await _monitoring.GetTutorDashboardAsync(GetUserId());
        return Ok(ApiResponse<TutorDashboardResponse>.Ok(result));
    }

    // ── Admin: Dashboard ──────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/monitoring/admin/dashboard
    ///
    /// Returns the full admin dashboard payload:
    /// - System KPIs (courses, active exams, enrolled students, violations today)
    /// - Global exam monitor table (all active sessions across all courses)
    /// - ECharts analytics (top violation types, 30-day trend, suspicious rate gauge)
    /// Admin role only.
    /// </summary>
    [HttpGet("api/monitoring/admin/dashboard")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<AdminDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var result = await _monitoring.GetAdminDashboardAsync();
        return Ok(ApiResponse<AdminDashboardResponse>.Ok(result));
    }
}
