namespace SHIELDON.Domain.Enums;

/// <summary>
/// Determines which grade type is used to compute the course leaderboard score.
/// </summary>
public enum LeaderboardCourseMetric
{
    TotalScore,       // Weighted sum of all published grades (exams + assignments)
    ExamAverage,      // Average of published exam grades only
    AssignmentAverage // Average of published assignment grades only
}
