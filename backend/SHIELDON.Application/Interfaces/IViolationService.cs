using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Violations.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Defines the contract for the Anti-Cheating Engine violation persistence service.
///
/// Responsibilities:
///   - Accept batches of violations from the student's browser (Anti-Cheat Engine)
///   - Provide tutor/admin read access to violations for monitoring and review
/// </summary>
public interface IViolationService
{
    /// <summary>
    /// Persists a batch of violations reported by the student's Anti-Cheat Engine.
    ///
    /// Called by the student's browser every 60 seconds during an exam,
    /// and one final time immediately before/after exam submission.
    ///
    /// Business rules:
    ///   - The attemptId in each violation must belong to the calling student
    ///   - Duplicate violations (same type + OccurredAt within 1 second) are silently ignored
    /// </summary>
    Task<ApiResponse<string>> LogViolationBatchAsync(
        BatchViolationRequest request,
        Guid studentId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all violations for a specific exam attempt.
    /// Available to: Tutor (own courses) and Admin (all).
    /// </summary>
    Task<ApiResponse<AttemptViolationSummary>> GetViolationsForAttemptAsync(
        Guid attemptId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Returns per-attempt violation summaries for all students who took a specific exam.
    /// Used in the monitoring dashboard to identify suspicious attempts.
    /// Available to: Tutor (own courses) and Admin (all).
    /// </summary>
    Task<ApiResponse<List<AttemptViolationSummary>>> GetViolationSummaryForExamAsync(
        Guid examId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);
}
