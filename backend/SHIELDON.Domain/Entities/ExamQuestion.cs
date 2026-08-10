using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Common;
using SHIELDON.Domain.Interfaces;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// A single question that lives in a course's centralized question bank.
/// Questions are no longer exam-specific; any exam in the course can draw from this bank.
/// </summary>
public class ExamQuestion : ITranslatable, ISoftDeletable
{
    public Guid Id { get; set; }

    // ── Soft Delete ──────────────────────────────────────────────
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // ── Ownership (course-level, not exam-level) ─────────────────
    public Guid CourseId { get; set; }

    // ── Content ─────────────────────────────────────────────────
    [Translatable]
    public string QuestionText { get; set; } = string.Empty;
    
    /// <summary>Optional image to display alongside the question for extra visual clarity.</summary>
    public string? ImageUrl { get; set; }
    
    public string? Translations { get; set; }
    public QuestionType Type { get; set; }
    public decimal Points { get; set; }
    public int OrderIndex { get; set; }
    public bool IsRandomized { get; set; } = true;

    // ── Tracking ─────────────────────────────────────────────────
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ────────────────────────────────────
    public Course? Course { get; set; }
    public ICollection<QuestionOption> Options { get; set; } = [];
}
