namespace SHIELDON.Application.Features.Monitoring.DTOs;

// ─────────────────────────────────────────────────────────────────────────────
// HEARTBEAT / PRESENCE
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Request body for the heartbeat endpoint (kept minimal on purpose).</summary>
public class HeartbeatRequest
{
    /// <summary>
    /// Optional — if true, the frontend is telling us this is a page refresh / session resume.
    /// This will be logged as a PageRefreshed presence event.
    /// </summary>
    public bool IsPageRefresh { get; set; } = false;
}

// ─────────────────────────────────────────────────────────────────────────────
// ATTEMPT TIMELINE (violations + presence events merged chronologically)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Complete info for one finished exam attempt, including all violations
/// and presence/connectivity events.
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
    /// <summary>
    /// Chronologically merged list of both violation events and presence events.
    /// Each entry has a Category field: "Violation" or "Presence".
    /// </summary>
    public List<TimelineEntry> Events { get; set; } = [];
}

/// <summary>
/// A single entry in the merged attempt timeline. Can represent either a
/// violation event or a connectivity/presence event.
/// </summary>
public class TimelineEntry
{
    /// <summary>"Violation" or "Presence"</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// For violations: the ViolationType enum name (e.g. "TabSwitch", "FocusLoss").
    /// For presence events: the PresenceEventType name (e.g. "Disconnected", "Reconnected", "PageRefreshed").
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>For violations: "Critical", "Medium", or "Minor". Empty for presence events.</summary>
    public string Severity { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }

    /// <summary>True if this violation caused an automatic exam force-submit.</summary>
    public bool WasAutoSubmit { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// VIOLATION SUMMARY (used for the chart endpoint)
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

/// <summary>
/// Query parameters for the paginated, searchable, sortable Exam Statistics table
/// on the admin dashboard.
/// </summary>
public class ExamStatisticsQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    /// <summary>Free-text search applied to Exam Title, Course Title, and Tutor Name.</summary>
    public string? Search { get; set; }

    /// <summary>Filter by specific Tutor ID</summary>
    public Guid? TutorId { get; set; }

    public string? SortColumn { get; set; } = "ScheduledAt";
    public string? SortDirection { get; set; } = "desc";
}

/// <summary>Full admin dashboard payload with platform-wide historical stats and analytics.</summary>
public class AdminDashboardResponse
{
    // ── KPI Cards — Row 1 ────────────────────────────────────────────────────
    /// <summary>Total number of currently active (non-archived) courses on the platform.</summary>
    public int TotalActiveCourses { get; set; }

    /// <summary>Total number of distinct exams that have at least one finished attempt.</summary>
    public int TotalCompletedExams { get; set; }

    /// <summary>Total number of exam submissions (all non-InProgress attempts).</summary>
    public int TotalSubmissions { get; set; }

    /// <summary>Total number of all violation events ever logged on the platform.</summary>
    public int TotalViolations { get; set; }

    // ── KPI Cards — Row 2 ────────────────────────────────────────────────────
    /// <summary>Total number of registered Student accounts.</summary>
    public int TotalStudents { get; set; }

    /// <summary>Total number of registered Tutor accounts.</summary>
    public int TotalTutors { get; set; }

    /// <summary>Number of exam attempts currently in progress right now.</summary>
    public int ActiveExamsInProgress { get; set; }

    /// <summary>Rate of force-submitted exams vs total submissions (0–100%).</summary>
    public decimal ForceSubmissionRate { get; set; }

    /// <summary>Total revenue collected from paid courses.</summary>
    public decimal TotalRevenueUSD { get; set; }

    // ── Charts ────────────────────────────────────────────────────────────────
    /// <summary>Violation counts grouped by course — for the "Violations by Course" chart.</summary>
    public List<CourseViolationStat> ViolationsByCourse { get; set; } = [];

    /// <summary>Submission outcome breakdown — for the "Global Submission Outcomes" chart.</summary>
    public List<SubmissionOutcomeStat> GlobalSubmissionOutcomes { get; set; } = [];

    /// <summary>Recent successful payments — for the "Recent Payments" bar chart.</summary>
    public List<RecentPaymentStat> RecentPayments { get; set; } = [];

    /// <summary>Top violation types for the ECharts horizontal bar chart.</summary>
    public List<ViolationTypeStat> TopViolationTypes { get; set; } = [];

    /// <summary>30-day activity trend for the ECharts line chart.</summary>
    public List<DailyActivityPoint> ActivityTrend { get; set; } = [];

    // ── Exam Statistics Table (server-side paginated + sorted) ─────────────────────────────────────────────────
    public List<ExamStatisticsRow> ExamStatistics { get; set; } = [];
    public int ExamStatisticsTotalCount { get; set; }
    public int ExamStatisticsPage { get; set; }
    public int ExamStatisticsPageSize { get; set; }
    public int ExamStatisticsTotalPages => ExamStatisticsPageSize > 0
        ? (int)Math.Ceiling((double)ExamStatisticsTotalCount / ExamStatisticsPageSize) : 0;
}

/// <summary>One row in the admin's expanded exam statistics table.</summary>
public class ExamStatisticsRow
{
    public Guid ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string TutorName { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime? ScheduledAt { get; set; }

    /// <summary>Total number of attempts started (all statuses).</summary>
    public int TotalAttempts { get; set; }

    /// <summary>Attempts with status Submitted or Graded.</summary>
    public int SubmittedCount { get; set; }

    /// <summary>Attempts force-submitted by the anti-cheat engine.</summary>
    public int ForceSubmittedCount { get; set; }

    /// <summary>Attempts still in progress.</summary>
    public int InProgressCount { get; set; }

    /// <summary>Total violations logged across all attempts for this exam.</summary>
    public int TotalViolations { get; set; }

    /// <summary>Average score across all scored attempts. Null if no scores yet.</summary>
    public decimal? AverageScore { get; set; }

    /// <summary>Percentage of submitted+graded attempts that passed. Null if no passing threshold set.</summary>
    public decimal? PassRate { get; set; }
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

/// <summary>Violation count per course — for the "Violations by Course" chart.</summary>
public class CourseViolationStat
{
    public string CourseTitle { get; set; } = string.Empty;
    public int ViolationCount { get; set; }
    public int CriticalCount { get; set; }
    public int MediumCount { get; set; }
    public int MinorCount { get; set; }
}

/// <summary>
/// Breakdown of submission outcomes across the entire platform.
/// Groups: Submitted (manual), ForceSubmitted, AutoExpired, InProgress.
/// </summary>
public class SubmissionOutcomeStat
{
    /// <summary>Outcome label: "Submitted", "ForceSubmitted", "AutoExpired", "InProgress".</summary>
    public string Outcome { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}


public class RecentPaymentStat
{
    public Guid PaymentId { get; set; }
    public decimal AmountUSD { get; set; }
    public DateTime PaidAt { get; set; }
    public string StudentName { get; set; } = string.Empty;
}
