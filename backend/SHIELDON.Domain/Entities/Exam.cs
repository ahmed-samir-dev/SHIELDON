using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Common;

namespace SHIELDON.Domain.Entities;

public class Exam : ITranslatable
{
    public Guid Id { get; set; }

    // ── Core Info ───────────────────────────────────────────────
    public Guid CourseId { get; set; }
    [Translatable]
    public string Title { get; set; } = string.Empty;
    
    [Translatable]
    public string? Instructions { get; set; }

    public string? Translations { get; set; }
    
    /// <summary>Time limit in minutes.</summary>
    public int TimeLimit { get; set; }
    public int MaxAttempts { get; set; } = 1;
    
    /// <summary>Percentage contribution to the final course grade (0.0 to 100.0)</summary>
    public decimal Weight { get; set; }

    /// <summary>Passing score percentage (0-100).</summary>
    public decimal PassScore { get; set; }

    // ── Status & Scheduling ─────────────────────────────────────
    public ExamStatus Status { get; set; } = ExamStatus.Draft;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? ScheduledEndAt { get; set; }
    
    // ── Results ─────────────────────────────────────────────────
    public ResultVisibility ResultVisibility { get; set; } = ResultVisibility.Immediate;
    public DateTime? ScheduledReleaseAt { get; set; }

    // ── Tracking ────────────────────────────────────────────────
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ────────────────────────────────────
    public Course? Course { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<ExamSelectionRule> SelectionRules { get; set; } = [];
    public ICollection<ExamAttempt> Attempts { get; set; } = [];
    public ICollection<GradeRecord> GradeRecords { get; set; } = [];
}
