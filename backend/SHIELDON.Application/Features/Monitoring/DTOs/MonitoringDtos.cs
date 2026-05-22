namespace SHIELDON.Application.Features.Monitoring.DTOs;

// ─────────────────────────────────────────────────────────────────────────────
// ATTEMPT TIMELINE (replaces the old merged presence+violation timeline)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Complete info for one finished exam attempt, including all violations.
/// Returned by GET /api/attempts/{id}/timeline.
/// </summary>
public class AttemptTimelineResponse
{
    public Guid AttemptId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string? StudentProfilePictureUrl { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? Score { get; set; }
    public int TotalViolations { get; set; }
    public int CriticalCount { get; set; }
    public int MediumCount { get; set; }
    public int MinorCount { get; set; }
    public List<ViolationTimelineEntry> Violations { get; set; } = [];
}

/// <summary>One violation event in the attempt timeline.</summary>
public class ViolationTimelineEntry
{
    public DateTime OccurredAt { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool WasAutoSubmit { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// VIOLATION SUMMARY (unchanged - used for the chart endpoint)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Aggregate statistics for all violations in one attempt - used for the summary cards row.</summary>
public class ViolationSummaryResponse
{
    public int TotalViolations { get; set; }
    public int CriticalCount { get; set; }
    public int MediumCount { get; set; }
    public int MinorCount { get; set; }

    /// <summary>How the exam ended: "Manual", "ForceSubmitted", "AutoExpired".</summary>
    public string SubmissionType { get; set; } = string.Empty;

    /// <summary>Each violation grouped for the chart (minute offset → count by severity).</summary>
    public List<ViolationChartPoint> ChartData { get; set; } = [];

    /// <summary>Full chronological list for the violations table.</summary>
    public List<ViolationTableRow> Violations { get; set; } = [];
}

/// <summary>A single data point for the ECharts violations-over-time chart.</summary>
public class ViolationChartPoint
{
    public int MinuteOffset { get; set; }
    public int CriticalCount { get; set; }
    public int MediumCount { get; set; }
    public int MinorCount { get; set; }
}

/// <summary>A single row in the violations table.</summary>
public class ViolationTableRow
{
    public DateTime OccurredAt { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool WasAutoSubmit { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// TUTOR DASHBOARD
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Full tutor dashboard payload with per-exam stats and paginated submission history.</summary>
public class TutorDashboardResponse
{
    /// <summary>One summary card per published exam the tutor owns.</summary>
    public List<ExamMonitoringSummary> ExamSummaries { get; set; } = [];

    /// <summary>Paginated list of recent finished attempts (Submitted/ForceSubmitted/Graded).</summary>
    public List<SubmissionRow> RecentSubmissions { get; set; } = [];

    public int TotalSubmissions { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    /// <summary>Violation type distribution for the ECharts doughnut chart.</summary>
    public List<ViolationTypeStat> ViolationTypeDistribution { get; set; } = [];
}

/// <summary>Per-exam aggregate stats shown as a summary card on the tutor dashboard.</summary>
public class ExamMonitoringSummary
{
    public Guid ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public int TotalEnrolled { get; set; }
    public int InProgressCount { get; set; }
    public int SubmittedCount { get; set; }
    public int ForceSubmittedCount { get; set; }
    public int NotStartedCount { get; set; }
    public int TotalViolations { get; set; }
    public int CriticalViolations { get; set; }
    public decimal? AverageScore { get; set; }
}

/// <summary>One row in the tutor's recent submissions table.</summary>
public class SubmissionRow
{
    public Guid AttemptId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string ExamTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public decimal? Score { get; set; }
    public int ViolationCount { get; set; }
    public string HighestSeverity { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────────────────────────────────────
// ADMIN DASHBOARD
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Full admin dashboard payload with platform-wide historical stats and analytics.</summary>
public class AdminDashboardResponse
{
    // KPI cards
    public int TotalActiveCourses { get; set; }
    public int TotalCompletedExams { get; set; }
    public int TotalSubmissions { get; set; }
    public int TotalViolations { get; set; }
    public decimal ForceSubmissionRate { get; set; }

    /// <summary>All exams with their aggregate stats (paginated on frontend).</summary>
    public List<ExamStatisticsRow> ExamStatistics { get; set; } = [];

    /// <summary>Top violation types for the ECharts horizontal bar chart.</summary>
    public List<ViolationTypeStat> TopViolationTypes { get; set; } = [];

    /// <summary>30-day activity trend for the ECharts line chart.</summary>
    public List<DailyActivityPoint> ActivityTrend { get; set; } = [];
}

/// <summary>One row in the admin's exam statistics table.</summary>
public class ExamStatisticsRow
{
    public Guid ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string TutorName { get; set; } = string.Empty;
    public int SubmittedCount { get; set; }
    public int ForceSubmittedCount { get; set; }
    public int InProgressCount { get; set; }
    public int TotalViolations { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// SHARED TYPES
// ─────────────────────────────────────────────────────────────────────────────

public class ViolationTypeStat
{
    public string ViolationType { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>One point in the 30-day activity trend chart.</summary>
public class DailyActivityPoint
{
    public DateOnly Date { get; set; }
    public int ExamCount { get; set; }
    public int ViolationCount { get; set; }
}
