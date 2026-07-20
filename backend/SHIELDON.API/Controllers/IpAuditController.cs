using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Security.DTOs;
using SHIELDON.Application.Interfaces;

namespace SHIELDON.API.Controllers;

/// <summary>
/// IP Audit Trail endpoints.
/// All endpoints are Admin/Tutor only — students cannot access IP data.
///
/// Routes:
///   GET /api/admin/audit-trail             — Paginated system-wide audit log (Admin only)
///   GET /api/users/{userId}/ip-logs        — IP logs for a specific user (Admin/Tutor)
///   GET /api/attempts/{attemptId}/ip-logs  — IP logs for a specific exam attempt (Admin/Tutor)
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,Tutor")]
public class IpAuditController : ControllerBase
{
    private readonly IIpAuditService _ipAuditService;

    public IpAuditController(IIpAuditService ipAuditService)
    {
        _ipAuditService = ipAuditService;
    }

    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

    // ── GET /api/admin/audit-trail ────────────────────────────────────────────

    /// <summary>
    /// Returns a paginated, filterable audit trail of all IP events across the system.
    /// Admin only — gives a system-wide view of login and exam network events.
    /// Supports filters: userId, eventType, isVpnOrProxy, isDuplicateSession,
    ///                   isNetworkChangeDuringExam, fromDate, toDate.
    /// </summary>
    [HttpGet("api/admin/audit-trail")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<AuditTrailPagedResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditTrail(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] bool? isVpnOrProxy = null,
        [FromQuery] bool? isDuplicateSession = null,
        [FromQuery] bool? isNetworkChangeDuringExam = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        Domain.Enums.IpAuditEventType? parsedEventType = null;
        if (!string.IsNullOrEmpty(eventType) &&
            Enum.TryParse<Domain.Enums.IpAuditEventType>(eventType, ignoreCase: true, out var parsed))
        {
            parsedEventType = parsed;
        }

        var query = new AuditTrailQueryParams
        {
            Page = page,
            PageSize = pageSize,
            UserId = userId,
            EventType = parsedEventType,
            IsVpnOrProxy = isVpnOrProxy,
            IsDuplicateSession = isDuplicateSession,
            IsNetworkChangeDuringExam = isNetworkChangeDuringExam,
            FromDate = fromDate,
            ToDate = toDate
        };

        var result = await _ipAuditService.GetAuditTrailAsync(query, ct);
        return Ok(ApiResponse<AuditTrailPagedResult>.Ok(result, "Audit trail retrieved successfully."));
    }

    // ── GET /api/users/{userId}/ip-logs ───────────────────────────────────────

    /// <summary>
    /// Returns all IP audit logs for a specific user, ordered newest-first.
    /// Admin or Tutor only.
    /// </summary>
    [HttpGet("api/users/{userId:guid}/ip-logs")]
    [ProducesResponseType(typeof(ApiResponse<List<IpAuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserIpLogs(Guid userId, CancellationToken ct = default)
    {
        var result = await _ipAuditService.GetLogsForUserAsync(userId, ct);
        return Ok(ApiResponse<List<IpAuditLogDto>>.Ok(result, "IP logs retrieved successfully."));
    }

    // ── GET /api/attempts/{attemptId}/ip-logs ─────────────────────────────────

    /// <summary>
    /// Returns all IP audit logs linked to a specific exam attempt, ordered chronologically.
    /// Used in the "Network Info" section of the Attempt Detail page.
    /// Admin or Tutor only.
    /// </summary>
    [HttpGet("api/attempts/{attemptId:guid}/ip-logs")]
    [ProducesResponseType(typeof(ApiResponse<List<IpAuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttemptIpLogs(Guid attemptId, CancellationToken ct = default)
    {
        var result = await _ipAuditService.GetLogsForAttemptAsync(attemptId, ct);
        return Ok(ApiResponse<List<IpAuditLogDto>>.Ok(result, "Attempt IP logs retrieved successfully."));
    }
}
