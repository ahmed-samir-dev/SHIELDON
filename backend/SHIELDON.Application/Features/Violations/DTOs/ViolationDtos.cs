using SHIELDON.Domain.Enums;

namespace SHIELDON.Application.Features.Violations.DTOs;

// ── Inbound: Student → API ─────────────────────────────────────────────────────

/// <summary>
/// Represents a single violation event reported by the client-side Anti-Cheat Engine.
/// The student's browser collects these during the exam and sends them in a batch.
/// </summary>
public record ViolationLogRequest(
    Guid AttemptId,
    ViolationType Type,
    ViolationSeverity Severity,
    string Description,
    DateTime OccurredAt,
    bool WasAutoSubmit
);

/// <summary>
/// Batch payload for logging multiple violations in a single HTTP call.
/// The Anti-Cheat Engine sends violations in batches every 60 seconds and
/// also sends the final batch when the exam is submitted or force-submitted.
/// </summary>
public record BatchViolationRequest(List<ViolationLogRequest> Violations);

// ── Outbound: API → Tutor/Admin ────────────────────────────────────────────────

/// <summary>
/// Summary of a single violation log record — returned to Tutor/Admin dashboards.
/// </summary>
public record ViolationLogResponse(
    Guid Id,
    Guid AttemptId,
    Guid StudentId,
    string StudentName,
    string StudentDisplayId,
    Guid ExamId,
    string ExamTitle,
    string Type,
    string Severity,
    string Description,
    DateTime OccurredAt,
    bool WasAutoSubmit,
    DateTime CreatedAt
);

/// <summary>
/// Aggregated violation summary for one student's attempt — used in the
/// tutor monitoring dashboard to quickly assess the integrity of an attempt.
/// </summary>
public record AttemptViolationSummary(
    Guid AttemptId,
    Guid StudentId,
    string StudentName,
    string StudentDisplayId,
    int TotalViolations,
    int CriticalCount,
    int MediumCount,
    int MinorCount,
    decimal StrikeScore,
    bool WasForceSubmitted,
    List<ViolationLogResponse> Violations
);
