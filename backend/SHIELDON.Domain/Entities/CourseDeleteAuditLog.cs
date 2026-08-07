namespace SHIELDON.Domain.Entities;

/// <summary>
/// Permanent audit record written by CourseService immediately before a course
/// is hard-deleted from the database.
///
/// Because the Course row is permanently destroyed, all identifying fields
/// (CourseId, CourseCode, CourseTitle) are stored as value snapshots rather than
/// foreign keys, ensuring this record can never become an orphan.
///
/// The DeletedByAdminId retains its FK to the Users table — the admin account
/// itself is never deleted, so this FK is safe.
///
/// All timestamps are stored in UTC.
/// </summary>
public class CourseDeleteAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // -- Who deleted ----------------------------------------------
    /// <summary>Admin user who triggered the hard delete.</summary>
    public Guid DeletedByAdminId { get; set; }

    /// <summary>
    /// Snapshot of the admin's full name at deletion time.
    /// Stored directly so the record remains accurate even if the admin's
    /// name is later updated.
    /// </summary>
    public string AdminFullName { get; set; } = string.Empty;

    // -- What was deleted (value snapshots — no FK to Courses) ----
    /// <summary>
    /// The original GUID of the deleted Course.
    /// Stored as a plain value — there is no FK constraint since the Course
    /// row no longer exists after deletion.
    /// </summary>
    public Guid CourseId { get; set; }

    /// <summary>Snapshot of the deleted course's unique short code (e.g. "CS360").</summary>
    public string CourseCode { get; set; } = string.Empty;

    /// <summary>Snapshot of the deleted course's display title.</summary>
    public string CourseTitle { get; set; } = string.Empty;

    // -- Timestamp ------------------------------------------------
    /// <summary>UTC timestamp of when the deletion was executed.</summary>
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;

    // -- Navigation -----------------------------------------------
    /// <summary>Navigation to the admin who performed the deletion.</summary>
    public User? DeletedByAdmin { get; set; }
}
