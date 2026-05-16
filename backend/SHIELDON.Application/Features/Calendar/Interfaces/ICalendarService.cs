using SHIELDON.Application.Features.Calendar.DTOs;

namespace SHIELDON.Application.Features.Calendar.Interfaces;

public interface ICalendarService
{
    Task<List<CalendarEventDto>> GetCalendarEventsAsync(Guid userId, DateTime start, DateTime end, CancellationToken ct);
    Task<CalendarEventDto> CreateCustomEventAsync(Guid userId, CreateCustomEventRequest request, CancellationToken ct);
    Task<CalendarEventDto> UpdateCustomEventAsync(Guid userId, Guid eventId, UpdateCustomEventRequest request, CancellationToken ct);
    Task<bool> DeleteCustomEventAsync(Guid userId, Guid eventId, CancellationToken ct);
}
