using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SHIELDON.API.Hubs;

/// <summary>
/// SignalR Hub for live leaderboard updates.
///
/// Clients join a course group to receive real-time leaderboard pushes.
/// Group naming: "leaderboard-course-{courseId}"
///
/// Events pushed by server:
///   - "LeaderboardUpdated": broadcast full LeaderboardResponse when ranks change.
/// </summary>
[Authorize]
public class LeaderboardHub : Hub
{
    /// <summary>
    /// Client calls this after connecting to subscribe to leaderboard updates for a course.
    /// </summary>
    public async Task JoinCourseLeaderboard(string courseId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"leaderboard-course-{courseId}");
    }

    /// <summary>
    /// Client calls this to unsubscribe from leaderboard updates for a course.
    /// </summary>
    public async Task LeaveCourseLeaderboard(string courseId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"leaderboard-course-{courseId}");
    }

    private Guid GetCurrentUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)
                 ?? Context.User?.FindFirst("sub");
        return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
    }
}
