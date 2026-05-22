using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Common;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// A single question that lives in a course's centralized question bank.
/// Questions are no longer exam-specific; any exam in the course can draw from this bank.
/// </summary>
public class ExamQuestion : ITranslatable
{
    public Guid Id { get; set; }

    // ── Ownership (course-level, not exam-level) ─────────────────
    public Guid CourseId { get; set; }

    // ── Content ─────────────────────────────────────────────────
    [Translatable]
    public string QuestionText { get; set; } = string.Empty;
    
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
