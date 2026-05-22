using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Users.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Implementation of IUserService.
/// Provides admin-only operations for listing, locking, and unlocking Tutors/Students.
/// Admins are always excluded from all queries.
/// </summary>
public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    // ─────────────────────────────────────────────────────────────────────────
    public async Task<PagedResponse<UserDetailDto>> GetUsersPaginatedAsync(
        UserFilterParams filters, CancellationToken ct = default)
    {
        // Base query: always exclude Admins
        var query = _db.Users
            .AsNoTracking()
            .Where(u => u.Role != UserRole.Admin);

        // Optional: role filter (Tutor or Student only)
        if (!string.IsNullOrWhiteSpace(filters.Role) &&
            Enum.TryParse<UserRole>(filters.Role, ignoreCase: true, out var parsedRole) &&
            parsedRole != UserRole.Admin)
        {
            query = query.Where(u => u.Role == parsedRole);
        }

        // Optional: account status filter
        if (!string.IsNullOrWhiteSpace(filters.Status) &&
            Enum.TryParse<AccountStatus>(filters.Status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(u => u.AccountStatus == parsedStatus);
        }

        // Optional: free-text search on name, email, StudentId, or TutorId
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var term = filters.Search.Trim().ToLower();
            query = query.Where(u =>
                (u.FirstName + " " + u.LastName).ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                (u.StudentId != null && u.StudentId.ToLower().Contains(term)) ||
                (u.TutorId != null && u.TutorId.ToLower().Contains(term)));
        }

        // Sort newest-first
        query = query.OrderByDescending(u => u.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var page = Math.Max(1, filters.Page);
        var pageSize = Math.Clamp(filters.PageSize, 1, 100);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDetailDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                ProfilePictureUrl = u.ProfilePictureUrl,
                Role = u.Role,
                StudentId = u.StudentId,
                TutorId = u.TutorId,
                AccountStatus = u.AccountStatus,
                FailedLoginAttempts = u.FailedLoginAttempts,
                LockedAt = u.LockedAt,
                EmailVerifiedAt = u.EmailVerifiedAt,
                LastLoginAt = u.LastLoginAt,
                HasCompletedOnboarding = u.HasCompletedOnboarding,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync(ct);

        return new PagedResponse<UserDetailDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<UserBasicDto>> GetTutorsAsync(CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Tutor && u.AccountStatus == AccountStatus.Active)
            .OrderBy(u => u.FirstName)
            .Select(u => new UserBasicDto(
                u.Id,
                u.FirstName + " " + u.LastName,
                u.Email,
                u.Role.ToString()
            ))
            .ToListAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public async Task LockUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("User", userId);

        if (user.Role == UserRole.Admin)
            throw new BusinessRuleException("Admins cannot be locked through this endpoint.");

        if (user.AccountStatus == AccountStatus.Locked)
            throw new BusinessRuleException("This account is already locked.");

        user.AccountStatus = AccountStatus.Locked;
        user.LockedAt = DateTime.UtcNow;
        user.FailedLoginAttempts = 0; // Explicitly set to 0 to differentiate from auto-lock
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public async Task UnlockUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("User", userId);

        if (user.AccountStatus != AccountStatus.Locked)
            throw new BusinessRuleException("This account is not locked.");

        user.AccountStatus = AccountStatus.Active;
        user.FailedLoginAttempts = 0;
        user.LockedAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}
