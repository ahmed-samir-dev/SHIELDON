using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Per-course leaderboard configuration (1-to-1 with Course).
/// Created automatically when a course is created; visibility defaults to false.
/// </summary>
public class LeaderboardSettings
{
    public Guid Id { get; set; }

    // ── Foreign Key ──────────────────────────────────────────────
    public Guid CourseId { get; set; }

    // ── Visibility Controls ──────────────────────────────────────
    /// <summary>
    /// When true, the Top-10 leaderboard is visible to enrolled students.
    /// Toggled by the assigned Tutor or Admin.
    /// </summary>
    public bool IsLeaderboardVisible { get; set; } = false;

    /// <summary>
    /// When true, a student can see their own rank card even if they are outside Top 10.
    /// This is only relevant while IsLeaderboardVisible = true.
    /// Toggled independently by the Tutor/Admin.
    /// </summary>
    public bool ShowStudentOwnRank { get; set; } = false;

    // ── Scoring Metric ───────────────────────────────────────────
    /// <summary>Which grade type to use when computing the leaderboard score.</summary>
    public LeaderboardCourseMetric ScoringMetric { get; set; } = LeaderboardCourseMetric.TotalScore;

    // ── Timestamps ───────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ───────────────────────────────────────────────
    public Course? Course { get; set; }
}
