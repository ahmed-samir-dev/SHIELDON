using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Monitoring.DTOs;
using SHIELDON.Application.Interfaces;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Phase 6 - Post-Exam Monitoring & Dashboards endpoints.
/// 
/// Tutor/Admin:
///   GET  /api/attempts/{attemptId}/timeline           - full violation timeline
///   GET  /api/attempts/{attemptId}/violations/summary - violation stats + chart data
///   GET  /api/monitoring/tutor/dashboard              - tutor dashboard data (historical)
///   GET  /api/monitoring/admin/dashboard              - admin dashboard data (historical)
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

    // ── Student: Heartbeat ─────────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/exam-attempts/{attemptId}/heartbeat
    ///
    /// Called by the exam engine every 30 seconds to signal the student is still online.
    /// If IsPageRefresh = true, logs a PageRefreshed presence event.
    /// Student role only — scoped to their own active attempt.
    /// </summary>
    [HttpPost("api/exam-attempts/{attemptId:guid}/heartbeat")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendHeartbeat(Guid attemptId, [FromBody] HeartbeatRequest request)
    {
        await _monitoring.ProcessHeartbeatAsync(attemptId, GetUserId(), request.IsPageRefresh);
        return Ok();
    }

    // ── Tutor/Admin: Session Timeline ─────────────────────────────────────────────

    /// <summary>
    /// GET /api/attempts/{attemptId}/timeline
    ///
    /// Returns the full, chronologically sorted violation timeline for one attempt.
    /// Tutor (own courses) and Admin only.
    /// </summary>
    [HttpGet("api/attempts/{attemptId:guid}/timeline")]
    [Authorize(Roles = "Tutor,Admin")]
    [ProducesResponseType(typeof(ApiResponse<AttemptTimelineResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimeline(Guid attemptId)
    {
        var result = await _monitoring.GetTimelineAsync(attemptId, GetUserId(), GetUserRole());
        return Ok(ApiResponse<AttemptTimelineResponse>.Ok(result));
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

    // ── Tutor: Dashboard ──────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/monitoring/tutor/dashboard
    ///
    /// Returns the full tutor dashboard payload:
    /// - Exam summary cards (aggregate stats)
    /// - Paginated recent submissions table
    /// - Violation type distribution (ECharts doughnut data)
    /// Tutor role only.
    /// </summary>
    [HttpGet("api/monitoring/tutor/dashboard")]
    [Authorize(Roles = "Tutor")]
    [ProducesResponseType(typeof(ApiResponse<TutorDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTutorDashboard(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        [FromQuery] string? search = null, 
        [FromQuery] string? status = null,
        [FromQuery] Guid? examId = null)
    {
        var result = await _monitoring.GetTutorDashboardAsync(GetUserId(), page, pageSize, search, status, examId);
        return Ok(ApiResponse<TutorDashboardResponse>.Ok(result));
    }

    /// <summary>
    /// GET /api/monitoring/admin/dashboard
    ///
    /// Returns the full admin dashboard payload:
    /// - System KPIs row 1: active courses, completed exams, total submissions, total violations
    /// - System KPIs row 2: total students, total tutors, active-in-progress, force-submission rate
    /// - Violations by Course chart data
    /// - Global Submission Outcomes chart data
    /// - Top violation types, 30-day activity trend
    /// - Paginated, searchable, sortable exam statistics table
    /// Admin role only.
    /// </summary>
    [HttpGet("api/monitoring/admin/dashboard")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<AdminDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAdminDashboard(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? tutorId = null,
        [FromQuery] string? sortColumn = "ScheduledAt",
        [FromQuery] string? sortDirection = "desc")
    {
        var queryParams = new ExamStatisticsQueryParams
        {
            Page          = page,
            PageSize      = pageSize,
            Search        = search,
            TutorId       = tutorId,
            SortColumn    = sortColumn,
            SortDirection = sortDirection
        };
        var result = await _monitoring.GetAdminDashboardAsync(queryParams);
        return Ok(ApiResponse<AdminDashboardResponse>.Ok(result));
    }

    [HttpGet("api/monitoring/admin/platform-activity")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PlatformActivityResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlatformActivity([FromQuery] int? days = null)
    {
        var result = await _monitoring.GetPlatformActivityAsync(days);
        return Ok(ApiResponse<PlatformActivityResponse>.Ok(result));
    }

    [HttpGet("api/monitoring/admin/payments-trend")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PaymentsTrendResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentsTrend([FromQuery] int? days = null)
    {
        var result = await _monitoring.GetPaymentsTrendAsync(days);
        return Ok(ApiResponse<PaymentsTrendResponse>.Ok(result));
    }
}
