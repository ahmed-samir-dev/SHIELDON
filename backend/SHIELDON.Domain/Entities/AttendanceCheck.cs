namespace SHIELDON.Domain.Entities;

/// <summary>
/// Represents a tutor-initiated QR attendance check for a specific course.
/// A check is "active" while the QR code is live and students can scan it.
/// The CurrentSecret rotates every 5 seconds via AttendanceRotationService.
/// </summary>
public class AttendanceCheck
{
    public Guid Id { get; set; }

    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public Guid TutorId { get; set; }
    public User Tutor { get; set; } = null!;

    /// <summary>Optional label e.g. "Week 3 Check". Defaults to date if not provided.</summary>
    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    /// <summary>True while the QR code is live and students can scan.</summary>
    public bool IsActive { get; set; }

    /// <summary>The rotating secret string used to generate the QR payload.</summary>
    public string CurrentSecret { get; set; } = string.Empty;

    /// <summary>UTC deadline for the current secret. After this, any submitted secret is rejected.</summary>
    public DateTime SecretExpiresAt { get; set; }

    // Navigation
    public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
}
