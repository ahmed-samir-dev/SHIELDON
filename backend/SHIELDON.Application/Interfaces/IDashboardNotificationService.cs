using System.Threading.Tasks;

namespace SHIELDON.Application.Interfaces;

public interface IDashboardNotificationService
{
    Task NotifyDashboardUpdatedAsync();
}
