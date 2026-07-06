using SHIELDON.Application.Features.Monitoring.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Defines all monitoring operations for the post-exam analytics dashboard.
/// Dashboards load on demand when the page is navigated to - no real-time polling.
/// </summary>
public interface IMonitoringService
{
    /// <summary>
    /// Processes a heartbeat from the student's exam browser tab.
    /// Updates LastHeartbeatAt, logs a Reconnected event if the student was disconnected,
    /// and optionally logs a PageRefreshed event.
    /// </summary>
    Task ProcessHeartbeatAsync(Guid attemptId, Guid studentId, bool isPageRefresh);

    /// <summary>
    /// Returns the full attempt timeline for one finished exam attempt.
    /// Contains the attempt's info and chronological list of all violations + presence events.
    /// Access: Tutor (own course) or Admin only.
    /// </summary>
    Task<AttemptTimelineResponse> GetTimelineAsync(Guid attemptId, Guid requesterId, string requesterRole);

    /// <summary>
    /// Returns aggregate violation statistics and chart data for one attempt.
    /// Access: Tutor (own course) or Admin only.
    /// </summary>
    Task<ViolationSummaryResponse> GetViolationSummaryAsync(Guid attemptId, Guid requesterId, string requesterRole);

    /// <summary>
    /// Returns the tutor dashboard: per-exam summary cards and paginated submission history.
    /// Data is computed fresh on each request - no caching or polling.
    /// </summary>
    Task<TutorDashboardResponse> GetTutorDashboardAsync(
        Guid tutorId,
        int page = 1,
        int pageSize = 10,
        string? search = null,
        string? status = null,
        Guid? examId = null);

    /// <summary>
    /// Returns the admin dashboard: platform-wide KPIs, exam statistics table,
    /// and ECharts analytics data (violation types, 30-day trend).
    /// </summary>
    Task<AdminDashboardResponse> GetAdminDashboardAsync();
}
