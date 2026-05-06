namespace SHIELDON.Domain.Entities;

/// <summary>
/// Represents a task or exercise published by a Tutor or Admin within a course.
/// Students view this, read the instructions, optionally download the reference file,
/// and then upload their own <see cref="AssignmentSubmission"/> as their answer.
/// </summary>
public class Assignment
{
    public Guid Id { get; set; }

    // ── Relationship Keys ────────────────────────────────────────
    /// <summary>The course this assignment belongs to.</summary>
    public Guid CourseId { get; set; }

    /// <summary>The Tutor or Admin who created this assignment.</summary>
    public Guid CreatedByUserId { get; set; }

    // ── Content ──────────────────────────────────────────────────
    /// <summary>Short title shown in the assignment list (e.g., "Week 3 Homework").</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional full instructions, problem description, or rubric.
    /// Supports multi-line plain text.
    /// </summary>
    public string? Instructions { get; set; }

    // ── Reference File (optional) ────────────────────────────────
    /// <summary>Original filename of the reference file as uploaded by the Tutor (e.g., "problem-set.pdf").</summary>
    public string? ReferenceFileName { get; set; }

    /// <summary>GUID-based disk filename to prevent collisions (e.g., "3f2a...pdf"). Null if no file attached.</summary>
    public string? ReferenceStoredFileName { get; set; }

    /// <summary>
    /// Relative path on disk to the reference file.
    /// Format: uploads/assignments/{courseId}/reference/{assignmentId}.{ext}
    /// Null if no file attached.
    /// </summary>
    public string? ReferenceFilePath { get; set; }

    /// <summary>Size in bytes of the reference file. Null if no file attached.</summary>
    public long? ReferenceFileSizeBytes { get; set; }

    /// <summary>MIME type of the reference file (e.g., "application/pdf"). Null if no file attached.</summary>
    public string? ReferenceContentType { get; set; }

    // ── Deadline ─────────────────────────────────────────────────
    /// <summary>
    /// Optional submission deadline in UTC.
    /// If set and the current time is past this value, students cannot submit or delete their submission.
    /// Only Tutor (assigned) or Admin can set/update/clear this.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Maximum points for grading student submissions. Default 100.</summary>
    public int MaxPoints { get; set; } = 100;

    /// <summary>Percentage contribution to the final course grade (0.0 to 100.0)</summary>
    public decimal Weight { get; set; }

    // ── Timestamps ──────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ────────────────────────────────────
    public Course? Course { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<AssignmentSubmission> Submissions { get; set; } = [];
    public ICollection<GradeRecord> GradeRecords { get; set; } = [];

}
