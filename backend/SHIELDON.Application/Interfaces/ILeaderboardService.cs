using SHIELDON.Application.Features.Leaderboard.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Leaderboard service contract.
///
/// Workflow:
///   1. Admin/Tutor enables visibility via UpdateLeaderboardSettingsAsync.
///   2. Students (and instructors) call GetLeaderboardAsync to see the current top-10.
///   3. Whenever grades are published/updated, callers should call ComputeAndBroadcastAsync
///      to recompute ranks, save snapshots, and push updates via LeaderboardHub.
/// </summary>
public interface ILeaderboardService
{
    // ── Student / Instructor Read ────────────────────────────────────────────

    /// <summary>
    /// Returns the current computed leaderboard for a course.
    ///
    /// - Students: only returned when IsLeaderboardVisible = true.
    /// - Instructors (Tutor/Admin): always returned regardless of visibility.
    ///
    /// The response always contains Top-10 rank positions (may include more students if ties).
    /// If ShowStudentOwnRank = true and the requesting student is outside Top-10,
    /// their own rank is also returned in StudentOwnRank.
    /// </summary>
    Task<LeaderboardResponse> GetLeaderboardAsync(
        Guid courseId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    // ── Tutor / Admin Settings ───────────────────────────────────────────────

    /// <summary>
    /// Returns current leaderboard settings for a course.
    /// Only Admin and the assigned Tutor may call this.
    /// If no settings row exists yet (e.g. old courses), one is created on first access.
    /// </summary>
    Task<LeaderboardSettingsResponse> GetSettingsAsync(
        Guid courseId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Updates leaderboard visibility and scoring metric for a course.
    /// Only Admin and the assigned Tutor may call this.
    /// After saving, recomputes ranks and broadcasts via SignalR if visibility is on.
    /// </summary>
    Task<LeaderboardSettingsResponse> UpdateLeaderboardSettingsAsync(
        Guid courseId,
        UpdateLeaderboardSettingsRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    // ── Internal / Trigger ───────────────────────────────────────────────────

    /// <summary>
    /// Recomputes the leaderboard rankings for a course, saves rank snapshots (upsert),
    /// and returns the updated LeaderboardResponse payload.
    ///
    /// Returns null if the leaderboard is not visible (no broadcast needed).
    /// The API controller is responsible for pushing the result to SignalR clients.
    ///
    /// Called internally after grade publication or settings change.
    /// </summary>
    Task<LeaderboardResponse?> ComputeAndBroadcastAsync(
        Guid courseId,
        CancellationToken ct = default);
}
