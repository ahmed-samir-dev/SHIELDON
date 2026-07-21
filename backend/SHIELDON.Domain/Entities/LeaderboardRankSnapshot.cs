namespace SHIELDON.Domain.Entities;

/// <summary>
/// Persists a leaderboard rank snapshot for a specific student in a course at a point in time.
/// Used to compute rank-change deltas (arrow up/down/same) when grades are updated.
/// Each course keeps only the LATEST snapshot per student (upsert pattern).
/// </summary>
public class LeaderboardRankSnapshot
{
    public Guid Id { get; set; }

    // ── Foreign Keys ─────────────────────────────────────────────
    public Guid CourseId { get; set; }
    public Guid StudentId { get; set; }

    // ── Snapshot Data ────────────────────────────────────────────
    /// <summary>
    /// The student's rank position at the time this snapshot was taken.
    /// Students with equal scores share the same rank (dense ranking).
    /// </summary>
    public int RankPosition { get; set; }

    /// <summary>The total score used to compute this rank.</summary>
    public decimal Score { get; set; }

    // ── Timestamp ────────────────────────────────────────────────
    public DateTime SnapshotAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ───────────────────────────────────────────────
    public Course? Course { get; set; }
    public User? Student { get; set; }
}
