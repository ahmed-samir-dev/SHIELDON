using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Tracks a student's enrollment request and status for a specific course.
/// Implements the cooldown and rejection-limit business rules from CLAUDE.md.
/// </summary>
public class CourseEnrollment
{
    public Guid Id { get; set; }

    // ── Relationship Keys ────────────────────────────────────────
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }

    // ── Status ──────────────────────────────────────────────────
    /// <summary>Current state of this enrollment request.</summary>
    public CourseEnrollmentStatus Status { get; set; } = CourseEnrollmentStatus.Pending;

    // ── Rejection Tracking ───────────────────────────────────────
    /// <summary>
    /// Total number of times this student has been rejected for this course.
    /// After 2 consecutive rejections: 24-hour cooldown is applied.
    /// After 3 total rejections: student is permanently blocked from requesting.
    /// </summary>
    public int RejectionCount { get; set; } = 0;

    /// <summary>
    /// UTC timestamp after which the student may submit a new request.
    /// Null when no cooldown is active.
    /// </summary>
    public DateTime? CooldownUntil { get; set; }

    /// <summary>Optional reason provided by the reviewer when rejecting.</summary>
    public string? RejectionReason { get; set; }

    // ── Review Info ──────────────────────────────────────────────
    /// <summary>The Admin or Tutor who approved/rejected this request.</summary>
    public Guid? ReviewedById { get; set; }

    /// <summary>UTC timestamp when the request was reviewed.</summary>
    public DateTime? ReviewedAt { get; set; }

    // ── Timestamps ──────────────────────────────────────────────
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ────────────────────────────────────
    public User? Student { get; set; }
    public Course? Course { get; set; }
    public User? ReviewedBy { get; set; }
}
