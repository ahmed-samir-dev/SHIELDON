using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Common;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Represents a course within the SHIELDON LMS.
/// A course is created by an Admin and assigned to a Tutor.
/// Students can request enrollment; a Tutor or Admin reviews the request.
/// </summary>
public class Course : ITranslatable
{
    public Guid Id { get; set; }

    // ── Core Info ───────────────────────────────────────────────
    /// <summary>Display name of the course (e.g., "Introduction to Networks").</summary>
    [Translatable]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Short unique code for the course (e.g., "CS101").
    /// Must be unique system-wide. Used for quick identification.
    /// </summary>
    public string CourseCode { get; set; } = string.Empty;

    /// <summary>Optional fee for the course in USD. Defaults to 0 (Free). Admin can set a fee on creation or later via edit.</summary>
    public decimal CourseFee { get; set; } = 0.00m;

    /// <summary>Optional longer description shown on the course page.</summary>
    [Translatable]
    public string? Description { get; set; }

    public string? Translations { get; set; }

    // ── Ownership ───────────────────────────────────────────────
    /// <summary>The Tutor (User) assigned to deliver this course. Null if not yet assigned.</summary>
    public Guid? AssignedTutorId { get; set; }

    /// <summary>The Admin (User) who created this course.</summary>
    public Guid CreatedByAdminId { get; set; }

    // ── Status ──────────────────────────────────────────────────
    /// <summary>When false, the course is hidden from students and unavailable for enrollment.</summary>
    public bool IsActive { get; set; } = true;

    // ── Timestamps ──────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ────────────────────────────────────
    public User? AssignedTutor { get; set; }
    public User? CreatedByAdmin { get; set; }
    public ICollection<CourseEnrollment> Enrollments { get; set; } = [];
    public ICollection<CourseMaterial> Materials { get; set; } = [];
    public ICollection<Announcement> Announcements { get; set; } = [];
    public ICollection<Assignment> Assignments { get; set; } = [];
    public ICollection<Exam> Exams { get; set; } = [];
    public ICollection<ExamQuestion> QuestionBankItems { get; set; } = [];
}
