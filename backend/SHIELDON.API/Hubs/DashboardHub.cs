using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SHIELDON.API.Hubs;

[Authorize(Policy = "RequireTutorOrAdmin")]
public class DashboardHub : Hub
{
    // Hub methods can be added here if clients need to send messages to the server.
    // For now, the server will broadcast to clients using IHubContext<DashboardHub>.
}
