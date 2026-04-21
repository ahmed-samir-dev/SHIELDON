using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Notifications.DTOs;
using SHIELDON.Application.Interfaces;

namespace SHIELDON.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require authentication
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<NotificationResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var response = await _notificationService.GetMyNotificationsAsync(userId, pageNumber, pageSize, ct);
        return Ok(ApiResponse<PagedResponse<NotificationResponse>>.Ok(response, "Notifications retrieved successfully."));
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct = default)
    {
        var userId = GetUserId();
        var count = await _notificationService.GetUnreadCountAsync(userId, ct);
        return Ok(ApiResponse<int>.Ok(count, "Unread count retrieved successfully."));
    }

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        await _notificationService.MarkAsReadAsync(id, userId, ct);
        return NoContent();
    }

    [HttpPatch("mark-all-read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct = default)
    {
        var userId = GetUserId();
        await _notificationService.MarkAllAsReadAsync(userId, ct);
        return NoContent();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAllNotifications(CancellationToken ct = default)
    {
        var userId = GetUserId();
        await _notificationService.DeleteAllNotificationsAsync(userId, ct);
        return NoContent();
    }
}
