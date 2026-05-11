using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Grades.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Grade Management Panel service contract.
///
/// Lifecycle:
///   - GradeRecord is auto-created on exam submit (ExamAttemptService) and assignment review (AssignmentService).
///   - Tutor/Admin: view all grades, set weights, override scores, add notes, publish.
///   - Student: view only their own published grades.
/// </summary>
public interface IGradeService
{
    // ── Tutor/Admin Views ────────────────────────────────────────────────────

    /// <summary>
    /// Returns a per-student grade summary table for a course.
    /// Only Admin and the assigned Tutor may call this.
    /// Each student row contains all their grade items (exams + assignments).
    /// </summary>
    Task<PagedResponse<CourseGradeSummaryResponse>> GetCourseGradesAsync(
        Guid courseId,
        GradeQueryParams query,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Update a single grade record's weight, score override, or notes.
    /// When weight is changed, ALL GradeRecord rows for the same exam/assignment are updated uniformly.
    /// Only Admin and the assigned Tutor may call this.
    /// </summary>
    Task<GradeItemResponse> UpdateGradeAsync(
        Guid gradeId,
        UpdateGradeRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Publish grade records for a course (selective or all).
    /// Only Admin and the assigned Tutor may call this.
    /// </summary>
    Task<string> PublishGradesAsync(
        Guid courseId,
        BulkPublishRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Streams a CSV export of all grade records for a course.
    /// Columns: Student, StudentID, Item, Type, Score, MaxScore, Weight, WeightedScore, Status, PublishedAt.
    /// Only Admin and the assigned Tutor may call this.
    /// </summary>
    Task<(byte[] CsvBytes, string FileName)> ExportGradesCsvAsync(
        Guid courseId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    // ── Student Views ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all published grade records for the requesting student across all courses.
    /// </summary>
    Task<IReadOnlyList<MyGradeItemResponse>> GetMyGradesAsync(
        Guid studentId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns published grade records for the requesting student in a specific course.
    /// </summary>
    Task<IReadOnlyList<MyGradeItemResponse>> GetMyGradesForCourseAsync(
        Guid courseId,
        Guid studentId,
        CancellationToken ct = default);
}
