namespace SHIELDON.Application.Features.Users.DTOs;

/// <summary>
/// Query parameters for the paginated users list endpoint.
/// Admins are always excluded server-side regardless of filters.
/// </summary>
public class UserFilterParams
{
    /// <summary>Current page (1-indexed).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Items per page. Fixed at 10 by convention but configurable.</summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Optional free-text search applied to: FullName, Email, StudentId, and TutorId.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Optional role filter. Accepted values: "Tutor", "Student".
    /// If null or empty, both Tutors and Students are returned.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Optional account status filter. Accepted values: "Active", "Locked", "Unverified".
    /// If null or empty, all statuses are returned.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Column to sort by. Accepted values (case-insensitive):
    /// Name, Email, Role, AccountStatus, EmailVerifiedAt, LastLoginAt, FailedLoginAttempts.
    /// Defaults to "Name".
    /// </summary>
    public string? SortColumn { get; set; } = "Name";

    /// <summary>"asc" or "desc". Defaults to "asc".</summary>
    public string? SortDirection { get; set; } = "asc";
}
