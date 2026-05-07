using SHIELDON.Application.Features.Monitoring.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Defines all monitoring operations for Phase 5.
/// Implemented in SHIELDON.Infrastructure.Services.MonitoringService.
/// </summary>
public interface IMonitoringService
{
    /// <summary>
    /// Logs a heartbeat from the student's browser. Updates LastHeartbeatAt on the attempt
    /// and inserts a HeartbeatReceived PresenceLog entry.
    /// </summary>
    Task LogHeartbeatAsync(Guid attemptId, Guid studentId);

    /// <summary>
    /// Returns the merged, chronologically sorted Session Timeline for one attempt.
    /// Combines PresenceLogs and ViolationLogs into a unified event stream.
    /// Access: Tutor (own course) or Admin only.
    /// </summary>
    Task<List<TimelineEventResponse>> GetTimelineAsync(Guid attemptId, Guid requesterId, string requesterRole);

    /// <summary>
    /// Returns aggregate violation statistics and chart data for one attempt.
    /// Access: Tutor (own course) or Admin only.
    /// </summary>
    Task<ViolationSummaryResponse> GetViolationSummaryAsync(Guid attemptId, Guid requesterId, string requesterRole);

    /// <summary>
    /// Returns the full tutor dashboard payload: active exams, live student grid,
    /// and violation distribution data for ECharts.
    /// </summary>
    Task<TutorDashboardResponse> GetTutorDashboardAsync(Guid tutorId);

    /// <summary>
    /// Returns the full admin dashboard payload: system KPIs, global exam monitor,
    /// ECharts analytics data.
    /// </summary>
    Task<AdminDashboardResponse> GetAdminDashboardAsync();

    /// <summary>
    /// Saves a manual review decision for a suspicious attempt.
    /// If Decision = MarkedAsCheating → sets the GradeRecord score to 0.
    /// If Decision = ReAttemptGranted → creates an approved ReattemptRequest.
    /// </summary>
    Task<ReviewDecisionResponse> SubmitReviewDecisionAsync(Guid attemptId, ReviewDecisionRequest request, Guid reviewerId);

    /// <summary>
    /// Immediately force-submits an active student exam session.
    /// Logs a TutorTerminated PresenceLog entry.
    /// Used by tutors/admins who spot suspicious live activity in the dashboard.
    /// </summary>
    Task TerminateSessionAsync(Guid attemptId, Guid terminatorId, string? reason);
}
