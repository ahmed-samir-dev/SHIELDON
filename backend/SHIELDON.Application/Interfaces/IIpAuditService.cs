using SHIELDON.Application.Features.Security.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// IP Audit Service contract.
/// Captures network-level context for every significant security event and
/// enforces session security policies.
///
/// Called from: AuthController (login), ExamAttemptsController (start/end), ViolationsController (report).
/// Lives in the Application layer - no dependency on ASP.NET Core.
/// </summary>
public interface IIpAuditService
{
    /// <summary>
    /// Records an IP audit log entry for a security event.
    /// Performs VPN/proxy detection, duplicate session detection,
    /// and network change detection (for exam events).
    /// Does NOT throw - all errors are logged silently to avoid breaking the primary flow.
    /// </summary>
    /// <param name="userId">The user performing the action.</param>
    /// <param name="ipAddress">Client IP address (IPv4 or IPv6). May be null if unavailable.</param>
    /// <param name="userAgent">Browser user-agent string. May be null.</param>
    /// <param name="eventType">The type of security event being recorded.</param>
    /// <param name="examAttemptId">Optional: links the log to a specific exam attempt.</param>
    Task LogAsync(
        Guid userId,
        string? ipAddress,
        string? userAgent,
        Domain.Enums.IpAuditEventType eventType,
        Guid? examAttemptId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all IP audit log entries for a specific user, ordered by OccurredAt descending.
    /// Admin and Tutor only - never returned to students.
    /// </summary>
    Task<List<IpAuditLogDto>> GetLogsForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns all IP audit log entries associated with a specific exam attempt,
    /// ordered by OccurredAt ascending (for timeline display on Attempt Detail page).
    /// Admin and Tutor only.
    /// </summary>
    Task<List<IpAuditLogDto>> GetLogsForAttemptAsync(Guid attemptId, CancellationToken ct = default);

    /// <summary>
    /// Returns a paginated, filterable audit trail of all IP logs system-wide.
    /// Admin only - used on the Admin Dashboard Audit Trail page.
    /// </summary>
    Task<AuditTrailPagedResult> GetAuditTrailAsync(AuditTrailQueryParams query, CancellationToken ct = default);
}
