using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SHIELDON.API.Hubs;
using SHIELDON.Application.Interfaces;

namespace SHIELDON.API.Services;

public class DashboardNotificationService : IDashboardNotificationService
{
    private readonly IHubContext<DashboardHub> _hubContext;
    public DashboardNotificationService(IHubContext<DashboardHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyDashboardUpdatedAsync()
    {
        await _hubContext.Clients.All.SendAsync("DashboardUpdated");
    }
}
