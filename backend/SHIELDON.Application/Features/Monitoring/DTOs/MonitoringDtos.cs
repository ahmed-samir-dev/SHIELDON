using SHIELDON.Domain.Enums;

namespace SHIELDON.Application.Features.Monitoring.DTOs;

// ─────────────────────────────────────────────────────────────────────────────
// REQUESTS
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Posted by the student's browser every 15 seconds to confirm they are still active.</summary>
public class HeartbeatRequest
{
    /// <summary>The ExamAttempt ID currently in progress.</summary>
    public Guid AttemptId { get; set; }
}

/// <summary>Submitted by a Tutor or Admin to record a manual review decision.</summary>
public class ReviewDecisionRequest
{
    /// <summary>The outcome of the review.</summary>
    public ReviewDecisionType Decision { get; set; }

    /// <summary>Optional free-text notes explaining the decision.</summary>
    public string? Notes { get; set; }
}

/// <summary>Submitted by a Tutor or Admin to immediately terminate an active student session.</summary>
public class TerminateSessionRequest
{
    /// <summary>Optional reason for termination. Will be stored as the PresenceLog Detail.</summary>
    public string? Reason { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// TIMELINE RESPONSES
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A single event in the unified Session Timeline.
/// Merges PresenceLogs and ViolationLogs into one chronologically sorted list.
/// </summary>
public class TimelineEventResponse
{
    /// <summary>UTC timestamp of the event.</summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>Category: "Presence" or "Violation".</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Human-readable event type (e.g. "ExamStarted", "TabSwitch").</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Severity level: "Info", "Minor", "Medium", "Critical". Null for presence events.</summary>
    public string? Severity { get; set; }

    /// <summary>Human-readable description of the event.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>True if this violation was the one that triggered an auto-force-submit.</summary>
    public bool WasAutoSubmit { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// VIOLATION SUMMARY
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Aggregate statistics for all violations in one attempt — used for the summary cards row.</summary>
public class ViolationSummaryResponse
{
    public int TotalViolations { get; set; }
    public int CriticalCount { get; set; }
    public int MediumCount { get; set; }
    public int MinorCount { get; set; }

    /// <summary>How the exam ended: "Manual", "ForceSubmitted", "AutoExpired".</summary>
    public string SubmissionType { get; set; } = string.Empty;

    /// <summary>Each violation, grouped for the chart (minute offset → count by severity).</summary>
    public List<ViolationChartPoint> ChartData { get; set; } = [];

    /// <summary>Full chronological list for the violations table.</summary>
    public List<ViolationTableRow> Violations { get; set; } = [];
}

/// <summary>A single data point for the ECharts violations-over-time chart.</summary>
public class ViolationChartPoint
{
    /// <summary>Minutes elapsed since exam start (X-axis).</summary>
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

/// <summary>Full tutor dashboard payload, covering all active exams and student statuses.</summary>
public class TutorDashboardResponse
{
    public List<ActiveExamSummary> ActiveExams { get; set; } = [];
    public List<LiveSessionRow> LiveSessions { get; set; } = [];
    public ViolationTypeDistribution ViolationDistribution { get; set; } = new();
}

/// <summary>Summary of one active exam shown in the Active Exams panel.</summary>
public class ActiveExamSummary
{
    public Guid ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public int InProgressCount { get; set; }
    public int SubmittedCount { get; set; }
    public int ForceSubmittedCount { get; set; }
    public int NotStartedCount { get; set; }
}

/// <summary>One row in the live student status grid.</summary>
public class LiveSessionRow
{
    public Guid AttemptId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string ExamTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;       // InProgress, Disconnected, Submitted, ForceSubmitted
    public int ViolationCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? LastHeartbeatAt { get; set; }
    public bool HasReviewDecision { get; set; }
}

/// <summary>Violation type distribution for the ECharts doughnut chart.</summary>
public class ViolationTypeDistribution
{
    public List<ViolationTypeStat> Items { get; set; } = [];
}

public class ViolationTypeStat
{
    public string ViolationType { get; set; } = string.Empty;
    public int Count { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// ADMIN DASHBOARD
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Full admin dashboard payload with system-wide KPIs and analytics.</summary>
public class AdminDashboardResponse
{
    // KPI cards
    public int TotalActiveCourses { get; set; }
    public int TotalOngoingExams { get; set; }
    public int TotalEnrolledStudents { get; set; }
    public int TotalViolationsToday { get; set; }
    public int TotalForceSubmittedToday { get; set; }

    // Global exam monitor table
    public List<GlobalExamRow> ActiveExamSessions { get; set; } = [];

    // ECharts data
    public List<ViolationTypeStat> TopViolationTypes { get; set; } = [];
    public List<DailyActivityPoint> ActivityTrend { get; set; } = [];
    public decimal SuspiciousSubmissionRatePercent { get; set; }
}

/// <summary>One row in the admin's global active exam session table.</summary>
public class GlobalExamRow
{
    public Guid ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string TutorName { get; set; } = string.Empty;
    public int StudentsInProgress { get; set; }
    public int TotalViolations { get; set; }
}

/// <summary>One point in the 30-day activity trend chart (X: date, Y: exam count).</summary>
public class DailyActivityPoint
{
    public DateOnly Date { get; set; }
    public int ExamCount { get; set; }
    public int ViolationCount { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// REVIEW DECISION RESPONSE
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Returned after a review decision is saved.</summary>
public class ReviewDecisionResponse
{
    public Guid DecisionId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime ReviewedAt { get; set; }
}
