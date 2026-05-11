using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Courses.DTOs;
using SHIELDON.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Manages course announcements.
/// - Admin / assigned Tutor: create, delete
/// - Admin / Tutor / Approved Student: list
/// All endpoints require JWT authentication.
/// </summary>
[ApiController]
[Route("api/courses/{courseId:guid}/announcements")]
[Authorize]
public class AnnouncementsController : ControllerBase
{
    private readonly IAnnouncementService _announcementService;

    public AnnouncementsController(IAnnouncementService announcementService)
    {
        _announcementService = announcementService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

    // ── Create ────────────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/courses/{courseId}/announcements
    /// Creates a new announcement in the course.
    /// Admin or assigned Tutor only.
    /// Priority "Important" pins the announcement at the top.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<AnnouncementResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateAnnouncement(
        Guid courseId,
        [FromBody] CreateAnnouncementRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _announcementService.CreateAnnouncementAsync(
            courseId, request, GetUserId(), GetUserRole(), cancellationToken);

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<AnnouncementResponse>.Ok(result, "Announcement posted successfully."));
    }

    // ── List ──────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/courses/{courseId}/announcements
    /// Returns all announcements for a course.
    /// Important-priority items appear pinned at the top.
    /// Students must be Approved-enrolled in the course.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AnnouncementResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnnouncements(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var result = await _announcementService.GetAnnouncementsAsync(
            courseId, GetUserId(), GetUserRole(), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<AnnouncementResponse>>.Ok(result, "Announcements retrieved successfully."));
    }

    // ── Delete ────────────────────────────────────────────────────────────

    /// <summary>
    /// DELETE /api/courses/{courseId}/announcements/{announcementId}
    /// Permanently deletes an announcement.
    /// Admin or assigned Tutor only.
    /// </summary>
    [HttpDelete("{announcementId:guid}")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAnnouncement(
        Guid courseId,
        Guid announcementId,
        CancellationToken cancellationToken)
    {
        await _announcementService.DeleteAnnouncementAsync(
            announcementId, GetUserId(), GetUserRole(), cancellationToken);

        return Ok(ApiResponse<object>.Ok("Announcement deleted successfully."));
    }
}
