using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Common;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Tutor")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/users/tutors
    /// Retrieves a list of all users with the Tutor role for assignment in courses.
    /// Accessible by Admins (to assign) and Tutors (to see their own assignment).
    /// </summary>
    [HttpGet("tutors")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserBasicResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTutors(CancellationToken cancellationToken)
    {
        var tutors = await _db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Tutor)
            .Select(t => new UserBasicResponse
            {
                Id = t.Id,
                FullName = $"{t.FirstName} {t.LastName}",
                Email = t.Email!
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IEnumerable<UserBasicResponse>>.Ok(tutors, "Tutors retrieved successfully."));
    }
}

public class UserBasicResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
