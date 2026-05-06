namespace SHIELDON.Domain.Entities;

/// <summary>
/// A student's answer file uploaded in response to a specific <see cref="Assignment"/>.
/// One student may have at most one active submission per assignment.
/// Submission is blocked if the assignment's DueDate has passed.
/// </summary>
public class AssignmentSubmission
{
    public Guid Id { get; set; }

    // ── Relationship Keys ────────────────────────────────────────
    /// <summary>The assignment this submission answers.</summary>
    public Guid AssignmentId { get; set; }

    /// <summary>The student (User) who submitted this file.</summary>
    public Guid StudentId { get; set; }

    // ── Submitted File ───────────────────────────────────────────
    /// <summary>Original filename as uploaded by the student (preserved for download).</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>GUID-based disk filename to prevent collisions (e.g., "9b1c...pdf").</summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// Relative path on disk to the student's submitted file.
    /// Format: uploads/assignments/{courseId}/submissions/{assignmentId}/{studentId}.{ext}
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>File size in bytes. Used for display.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>MIME type of the submitted file (e.g., "application/pdf"). Used for secure serving.</summary>
    public string ContentType { get; set; } = string.Empty;

    // ── Timestamps ──────────────────────────────────────────────
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Review / Grading (set by Tutor/Admin) ───────────────────
    /// <summary>Points awarded by the tutor. Null until reviewed.</summary>
    public decimal? PointsAwarded { get; set; }

    /// <summary>Optional tutor feedback / comments on the submission.</summary>
    public string? Feedback { get; set; }

    /// <summary>When the submission was reviewed. Null until reviewed.</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>The Tutor or Admin who reviewed this submission.</summary>
    public Guid? ReviewedById { get; set; }

    // ── Navigation Properties ────────────────────────────────────
    public Assignment? Assignment { get; set; }
    public User? Student { get; set; }
    public User? ReviewedBy { get; set; }
}
