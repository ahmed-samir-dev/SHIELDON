using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

public class Exam
{
    public Guid Id { get; set; }

    // ── Core Info ───────────────────────────────────────────────
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    
    /// <summary>Time limit in minutes.</summary>
    public int TimeLimit { get; set; }
    public int MaxAttempts { get; set; } = 1;
    
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
