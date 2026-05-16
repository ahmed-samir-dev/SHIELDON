using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Calendar.DTOs;
using SHIELDON.Application.Features.Calendar.Interfaces;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CalendarController : ControllerBase
{
    private readonly ICalendarService _calendarService;

    public CalendarController(ICalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Gets unified calendar events for the current user within a date range.
    /// </summary>
    [HttpGet("events")]
    [ProducesResponseType(typeof(ApiResponse<List<CalendarEventDto>>), 200)]
    public async Task<IActionResult> GetEvents([FromQuery] DateTime start, [FromQuery] DateTime end, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _calendarService.GetCalendarEventsAsync(userId, start, end, ct);

        return Ok(ApiResponse<List<CalendarEventDto>>.Ok(result, "Events retrieved successfully."));
    }

    /// <summary>
    /// Creates a custom event. (Admins/Tutors only)
    /// </summary>
    [HttpPost("events/custom")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<CalendarEventDto>), 201)]
    public async Task<IActionResult> CreateCustomEvent([FromBody] CreateCustomEventRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _calendarService.CreateCustomEventAsync(userId, request, ct);

        return Created($"/api/calendar/events", ApiResponse<CalendarEventDto>.Ok(result, "Event created successfully."));
    }

    /// <summary>
    /// Updates a custom event. (Admins/Tutors only)
    /// </summary>
    [HttpPut("events/custom/{eventId}")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<CalendarEventDto>), 200)]
    public async Task<IActionResult> UpdateCustomEvent([FromRoute] Guid eventId, [FromBody] UpdateCustomEventRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _calendarService.UpdateCustomEventAsync(userId, eventId, request, ct);

        return Ok(ApiResponse<CalendarEventDto>.Ok(result, "Event updated successfully."));
    }

    /// <summary>
    /// Deletes a custom event. (Admins/Tutors only)
    /// </summary>
    [HttpDelete("events/custom/{eventId}")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> DeleteCustomEvent([FromRoute] Guid eventId, CancellationToken ct)
    {
        var userId = GetUserId();
        await _calendarService.DeleteCustomEventAsync(userId, eventId, ct);

        return Ok(ApiResponse<bool>.Ok(true, "Event deleted successfully."));
    }
}
