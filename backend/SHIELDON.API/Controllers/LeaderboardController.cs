using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SHIELDON.API.Hubs;
using SHIELDON.Application.Features.Leaderboard.DTOs;
using SHIELDON.Application.Interfaces;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Leaderboard endpoints.
///
/// GET  /api/courses/{courseId}/leaderboard          → View leaderboard (student + instructor)
/// GET  /api/courses/{courseId}/leaderboard/settings → View settings (instructor only)
/// PUT  /api/courses/{courseId}/leaderboard/settings → Update settings (instructor only)
/// POST /api/courses/{courseId}/leaderboard/refresh  → Manually trigger recompute (instructor only)
/// </summary>
[ApiController]
[Route("api/courses/{courseId:guid}/leaderboard")]
[Authorize]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboard;
    private readonly IHubContext<LeaderboardHub> _hub;

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

    public LeaderboardController(ILeaderboardService leaderboard, IHubContext<LeaderboardHub> hub)
    {
        _leaderboard = leaderboard;
        _hub = hub;
    }

    // ── GET /api/courses/{courseId}/leaderboard ──────────────────────────────

    /// <summary>
    /// Returns the current Top-10 leaderboard for a course.
    /// Students: only accessible when IsLeaderboardVisible = true.
    /// Tutors/Admins: always accessible for preview purposes.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(LeaderboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLeaderboard(Guid courseId, CancellationToken ct)
    {
        var result = await _leaderboard.GetLeaderboardAsync(
            courseId, GetUserId(), GetUserRole(), ct);
        return Ok(result);
    }

    // ── GET /api/courses/{courseId}/leaderboard/settings ────────────────────

    /// <summary>
    /// Returns leaderboard configuration for a course.
    /// Only the assigned Tutor and Admins may access this.
    /// </summary>
    [HttpGet("settings")]
    [ProducesResponseType(typeof(LeaderboardSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSettings(Guid courseId, CancellationToken ct)
    {
        var result = await _leaderboard.GetSettingsAsync(
            courseId, GetUserId(), GetUserRole(), ct);
        return Ok(result);
    }

    // ── PUT /api/courses/{courseId}/leaderboard/settings ────────────────────

    /// <summary>
    /// Updates leaderboard visibility settings.
    /// Only the assigned Tutor and Admins may update.
    /// After update, if leaderboard is visible, broadcasts updated rankings via SignalR.
    /// </summary>
    [HttpPut("settings")]
    [ProducesResponseType(typeof(LeaderboardSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSettings(
        Guid courseId,
        [FromBody] UpdateLeaderboardSettingsRequest request,
        CancellationToken ct)
    {
        var result = await _leaderboard.UpdateLeaderboardSettingsAsync(
            courseId, request, GetUserId(), GetUserRole(), ct);

        // Broadcast updated leaderboard to connected clients if visible
        if (result.IsLeaderboardVisible)
        {
            var payload = await _leaderboard.ComputeAndBroadcastAsync(courseId, ct);
            if (payload != null)
            {
                await _hub.Clients
                    .Group($"leaderboard-course-{courseId}")
                    .SendAsync("LeaderboardUpdated", payload, ct);
            }
        }

        return Ok(result);
    }

    // ── POST /api/courses/{courseId}/leaderboard/refresh ────────────────────

    /// <summary>
    /// Manually triggers leaderboard recomputation and broadcasts updated rankings.
    /// Useful after grade edits. Only Tutor/Admin may call this.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RefreshLeaderboard(Guid courseId, CancellationToken ct)
    {
        // Only instructors can manually refresh
        var role = GetUserRole();
        if (role == "Student")
            return Forbid();

        var payload = await _leaderboard.ComputeAndBroadcastAsync(courseId, ct);
        if (payload != null)
        {
            await _hub.Clients
                .Group($"leaderboard-course-{courseId}")
                .SendAsync("LeaderboardUpdated", payload, ct);
            return Ok(new { message = "Leaderboard refreshed and broadcast to clients." });
        }

        return Ok(new { message = "Leaderboard is not visible; no broadcast performed." });
    }
}
