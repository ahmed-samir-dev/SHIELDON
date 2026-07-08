using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Users.DTOs;
using SHIELDON.Application.Interfaces;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Admin-only endpoints for managing system users (Tutors and Students).
/// Admins themselves are always excluded from all results.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// GET /api/users
    ///
    /// Returns a paginated, searchable, filterable, and sortable list of all Tutors and Students.
    /// Admins are never returned. Supports:
    ///   - ?search=        (name, email, StudentId, TutorId)
    ///   - ?role=          Tutor | Student
    ///   - ?status=        Active | Locked | Unverified
    ///   - ?sortColumn=    Name | Email | Role | AccountStatus | EmailVerifiedAt | LastLoginAt | FailedLoginAttempts
    ///   - ?sortDirection= asc | desc
    ///   - ?page=          (default 1)
    ///   - ?pageSize=      (default 10)
    /// Admin role only.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<UserDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sortColumn = "Name",
        [FromQuery] string? sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var filters = new UserFilterParams
        {
            Page          = page,
            PageSize      = pageSize,
            Search        = search,
            Role          = role,
            Status        = status,
            SortColumn    = sortColumn,
            SortDirection = sortDirection
        };

        var result = await _userService.GetUsersPaginatedAsync(filters, cancellationToken);
        return Ok(ApiResponse<PagedResponse<UserDetailDto>>.Ok(result, "Users retrieved successfully."));
    }

    /// <summary>
    /// GET /api/users/tutors
    ///
    /// Returns a lightweight list of all active Tutors.
    /// Admin role only.
    /// </summary>
    [HttpGet("tutors")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserBasicDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTutors(CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetTutorsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UserBasicDto>>.Ok(result, "Tutors retrieved successfully."));
    }

    /// <summary>
    /// POST /api/users/{id}/lock
    ///
    /// Locks an active Tutor or Student account.
    /// The user will be unable to log in until unlocked.
    /// Admin role only.
    /// </summary>
    [HttpPost("{id:guid}/lock")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LockUser(Guid id, CancellationToken cancellationToken)
    {
        await _userService.LockUserAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok("Account has been locked successfully."));
    }

    /// <summary>
    /// POST /api/users/{id}/unlock
    ///
    /// Unlocks a locked Tutor or Student account.
    /// Resets FailedLoginAttempts to 0 and restores Active status.
    /// Admin role only.
    /// </summary>
    [HttpPost("{id:guid}/unlock")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnlockUser(Guid id, CancellationToken cancellationToken)
    {
        await _userService.UnlockUserAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok("Account has been unlocked successfully."));
    }
}
