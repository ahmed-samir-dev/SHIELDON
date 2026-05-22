namespace SHIELDON.Domain.Entities;

/// <summary>
/// Records that a specific student was marked present in an AttendanceCheck.
/// Existence of a record = present. Absence of a record = absent.
/// One record per (AttendanceCheckId, StudentId) pair - enforced by DB unique index.
/// </summary>
public class AttendanceRecord
{
    public Guid Id { get; set; }

    public Guid AttendanceCheckId { get; set; }
    public AttendanceCheck AttendanceCheck { get; set; } = null!;

    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;

    public DateTime ScannedAt { get; set; }

    /// <summary>True if the tutor manually toggled this student as present (camera fallback).</summary>
    public bool IsManual { get; set; }
}
