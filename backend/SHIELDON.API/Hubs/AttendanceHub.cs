using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SHIELDON.API.Hubs;

/// <summary>
/// SignalR Hub for real-time attendance events.
/// - Tutors join group "attendance-tutor-{checkId}" to receive QrUpdated pushes.
/// - Students join group "attendance-check-{checkId}" to receive AttendanceMarked events.
/// </summary>
[Authorize]
public class AttendanceHub : Hub
{
    /// <summary>Tutor joins the live QR stream for a specific check.</summary>
    public async Task JoinCheckAsHost(string checkId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"attendance-tutor-{checkId}");
    }

    /// <summary>Student joins to receive confirmation when they are marked.</summary>
    public async Task JoinCheckAsStudent(string checkId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"attendance-check-{checkId}");
    }

    public async Task LeaveCheck(string checkId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"attendance-tutor-{checkId}");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"attendance-check-{checkId}");
    }

    private Guid GetCurrentUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)
                 ?? Context.User?.FindFirst("sub");
        return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
    }
}
