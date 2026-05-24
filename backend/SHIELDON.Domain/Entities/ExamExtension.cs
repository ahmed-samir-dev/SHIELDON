namespace SHIELDON.Domain.Entities;

/// <summary>
/// Grants a specific student a personal extension window to access an exam
/// that has already expired globally. Created when a Tutor approves a Re-open Request.
/// The student can enter the exam only between GrantedAt and ExtendedEndTime.
/// </summary>
public class ExamExtension
{
    public Guid Id { get; set; }

    /// <summary>The student who is granted the extension.</summary>
    public Guid StudentId { get; set; }

    /// <summary>The exam being re-opened for this specific student.</summary>
    public Guid ExamId { get; set; }

    /// <summary>
    /// The specific datetime (UTC) until which this student may enter and submit the exam.
    /// Set by the Tutor (typically 24h or 48h from approval).
    /// </summary>
    public DateTime ExtendedEndTime { get; set; }

    /// <summary>The ReattemptRequest that triggered this extension.</summary>
    public Guid SourceRequestId { get; set; }

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ────────────────────────────────────────────────
    public User? Student { get; set; }
    public Exam? Exam { get; set; }
    public ReattemptRequest? SourceRequest { get; set; }
}
