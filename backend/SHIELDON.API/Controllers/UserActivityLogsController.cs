using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Common;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Admin-only controller for querying expanded System Security and User Activity Logs.
/// </summary>
[ApiController]
[Route("api/admin/user-activity-logs")]
[Authorize(Roles = "Admin")]
public class UserActivityLogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public UserActivityLogsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Retrieves paginated system security & user activity logs with category, action, and user filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null,
        [FromQuery] string? action = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? userId = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var q = _db.UserActivityLogs.AsNoTracking().AsQueryable();

        if (userId.HasValue)
            q = q.Where(x => x.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(x => x.Category == category.Trim().ToUpper());

        if (!string.IsNullOrWhiteSpace(action))
            q = q.Where(x => x.Action.Contains(action.Trim()));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(x =>
                (x.UserEmail != null && x.UserEmail.ToLower().Contains(term)) ||
                (x.Description != null && x.Description.ToLower().Contains(term)) ||
                (x.IpAddress != null && x.IpAddress.Contains(term)));
        }

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.UserEmail,
                x.UserRole,
                x.Category,
                x.Action,
                x.Description,
                x.EntityId,
                x.EntityType,
                x.MetadataJson,
                x.IpAddress,
                x.UserAgent,
                x.CreatedAt
            })
            .ToListAsync(ct);

        var response = new PagedResponse<object>
        {
            Items = items.Cast<object>().ToList(),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PagedResponse<object>>.Ok(response, "User activity logs retrieved successfully."));
    }
}
