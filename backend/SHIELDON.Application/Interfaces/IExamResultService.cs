using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Exams.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Manages exam result retrieval, short-answer manual grading, and result publication.
/// </summary>
public interface IExamResultService
{
    // ── Student ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the full result for a specific attempt (student sees own; tutor/admin see any).
    /// Enforces ResultVisibility: student only sees data when published or Immediate.
    /// </summary>
    Task<ApiResponse<ExamResultResponse>> GetAttemptResultAsync(
        Guid attemptId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all attempts for a given exam made by the requesting student.
    /// Used for "My Results" frontend view.
    /// </summary>
    Task<ApiResponse<IReadOnlyList<ExamAttemptSummaryDto>>> GetStudentAttemptsAsync(
        Guid examId,
        Guid requestingUserId,
        CancellationToken ct = default);

    // ── Tutor / Admin ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all attempts for a given exam — for the tutor results panel.
    /// </summary>
    Task<ApiResponse<IReadOnlyList<ExamAttemptSummaryDto>>> GetExamAttemptsAsync(
        Guid examId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Assigns points to short-answer questions in an attempt and finalises grading.
    /// </summary>
    Task<ApiResponse<string>> GradeShortAnswersAsync(
        Guid attemptId,
        GradeShortAnswerRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Publishes results for all (or selected) students on a ManualRelease exam.
    /// </summary>
    Task<ApiResponse<string>> ReleaseResultsAsync(
        Guid examId,
        ReleaseResultsRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);
}
