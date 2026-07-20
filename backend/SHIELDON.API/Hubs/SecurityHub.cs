using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SHIELDON.API.Hubs;

/// <summary>
/// SignalR Hub for real-time security events (e.g., concurrent session invalidation and force logout).
/// Each user joins a SignalR group named after their User ID, allowing targeted security notifications.
/// </summary>
[Authorize]
public class SecurityHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        if (userId != Guid.Empty)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
        }
        await base.OnConnectedAsync();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var guid) ? guid : Guid.Empty;
    }
}
