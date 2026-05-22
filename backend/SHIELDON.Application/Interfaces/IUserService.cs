using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Users.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Admin-only service for managing system users (Tutors and Students).
/// Admins are never exposed through this service.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Returns a paginated, filtered list of Tutors and Students.
    /// Admins are always excluded from results.
    Task<PagedResponse<UserDetailDto>> GetUsersPaginatedAsync(UserFilterParams filters, CancellationToken ct = default);

    /// <summary>
    /// Returns a list of all active Tutors (basic details) for dropdowns.
    /// </summary>
    Task<IReadOnlyList<UserBasicDto>> GetTutorsAsync(CancellationToken ct = default);

    /// <summary>
    /// Locks an active user account. Sets AccountStatus to Locked and records LockedAt timestamp.
    /// </summary>
    Task LockUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Unlocks a locked user account. Sets AccountStatus to Active (or Unverified) and resets failed attempts.
    /// </summary>
    Task UnlockUserAsync(Guid userId, CancellationToken ct = default);
}
