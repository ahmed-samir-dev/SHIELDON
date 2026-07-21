namespace SHIELDON.Application.Features.Leaderboard.DTOs;

// ── Response DTOs ─────────────────────────────────────────────────────────────

/// <summary>
/// Represents a single student row in the Top-10 leaderboard.
/// RankDelta: positive = climbed, negative = fell, 0 = no change, null = new entry.
/// </summary>
public record LeaderboardEntryResponse(
    int Rank,                      // Dense rank (ties share same position)
    Guid StudentId,
    string StudentName,
    string? StudentDisplayId,      // e.g. "STU-0042"
    string? AvatarUrl,             // Profile image URL
    decimal Score,
    int? RankDelta                 // null = no previous snapshot
);

/// <summary>
/// Full leaderboard response returned to clients.
/// Includes Top-10 entries, visibility settings, and the requesting student's own rank (if applicable).
/// </summary>
public record LeaderboardResponse(
    Guid CourseId,
    string CourseTitle,
    string ScoringMetric,                          // "TotalScore" | "ExamAverage" | "AssignmentAverage"
    bool IsLeaderboardVisible,
    bool ShowStudentOwnRank,
    IReadOnlyList<LeaderboardEntryResponse> TopEntries,  // Up to 10 rank positions (may have >10 students due to ties)
    LeaderboardEntryResponse? StudentOwnRank,            // null if student is in Top-10 or ShowStudentOwnRank = false
    DateTime GeneratedAt
);

/// <summary>
/// Settings returned when admin/tutor views leaderboard configuration.
/// </summary>
public record LeaderboardSettingsResponse(
    Guid Id,
    Guid CourseId,
    bool IsLeaderboardVisible,
    bool ShowStudentOwnRank,
    string ScoringMetric,
    DateTime UpdatedAt
);

// ── Request DTOs ─────────────────────────────────────────────────────────────

/// <summary>Tutor/Admin updates leaderboard visibility settings for a course.</summary>
public record UpdateLeaderboardSettingsRequest(
    bool IsLeaderboardVisible,
    bool ShowStudentOwnRank,
    string ScoringMetric  // "TotalScore" | "ExamAverage" | "AssignmentAverage"
);
