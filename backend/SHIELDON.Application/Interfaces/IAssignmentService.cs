using SHIELDON.Application.Features.Courses.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Assignment management service contract.
/// Handles the full assignment lifecycle:
///   - Tutor/Admin creates an Assignment (task + optional reference file + optional due date)
///   - Students submit their answer file as an AssignmentSubmission
///   - Tutor/Admin reviews all submissions and can download individually or as a ZIP
///
/// RBAC enforced inside every method:
///   - Create/Update/Delete Assignment → Tutor (assigned) or Admin only
///   - Submit/Delete own submission → enrolled Student only (deadline guard applied)
///   - Download submissions → Student owns | Tutor assigned to course | Admin
///   - Bulk ZIP download → Tutor (assigned) or Admin only
/// </summary>
public interface IAssignmentService
{
    // ── Assignment CRUD ──────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new assignment in a course.
    /// An optional reference file (max 50 MB) may accompany the request.
    /// Only Admin or the assigned Tutor of the course may call this.
    /// </summary>
    Task<AssignmentResponse> CreateAssignmentAsync(
        Guid courseId,
        CreateAssignmentRequest request,
        UploadedFileDto? referenceFile,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all assignments for a course.
    /// Students: must be Approved-enrolled; MySubmission is populated with their own submission.
    /// Tutor/Admin: always allowed; SubmissionCount is populated.
    /// </summary>
    Task<IReadOnlyList<AssignmentResponse>> GetAssignmentsAsync(
        Guid courseId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an assignment's title, instructions, or due date.
    /// Only Admin or the assigned Tutor of the course may call this.
    /// </summary>
    Task<AssignmentResponse> UpdateAssignmentAsync(
        Guid assignmentId,
        UpdateAssignmentRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Permanently deletes an assignment and all its student submissions.
    /// Physical files (reference + all submission files) are removed from disk.
    /// Only Admin or the assigned Tutor of the course may call this.
    /// </summary>
    Task DeleteAssignmentAsync(
        Guid assignmentId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    // ── Reference File ───────────────────────────────────────────────────────

    /// <summary>
    /// Streams the reference file attached to an assignment.
    /// Available to enrolled students, the assigned Tutor, and Admin.
    /// Returns 404 if no reference file is attached.
    /// </summary>
    Task<(Stream FileStream, string ContentType, string FileName)> DownloadReferenceFileAsync(
        Guid assignmentId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    // ── Submissions ──────────────────────────────────────────────────────────

    /// <summary>
    /// Uploads a student's submission file for an assignment.
    /// Guards: student must be Approved-enrolled; assignment DueDate must not have passed;
    /// student must not have submitted previously (409 if duplicate).
    /// Max file size: 100 MB.
    /// </summary>
    Task<AssignmentSubmissionResponse> SubmitAssignmentAsync(
        Guid assignmentId,
        Guid studentId,
        UploadedFileDto file,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a student's submission.
    /// Student: can only delete their own and only before the assignment's DueDate.
    /// Tutor (assigned) / Admin: can delete any submission at any time.
    /// Physical file is removed from disk.
    /// </summary>
    Task DeleteSubmissionAsync(
        Guid submissionId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    // ── Submission Downloads ─────────────────────────────────────────────────

    /// <summary>
    /// Returns all submissions for a specific assignment.
    /// Only Tutor (assigned to the course) and Admin may call this.
    /// </summary>
    Task<IReadOnlyList<AssignmentSubmissionResponse>> GetSubmissionsAsync(
        Guid assignmentId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Streams a single student's submission file.
    /// Student: own submission only. Tutor (assigned) / Admin: any submission.
    /// </summary>
    Task<(Stream FileStream, string ContentType, string FileName)> DownloadSubmissionAsync(
        Guid submissionId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Packages all submission files for an assignment into an in-memory ZIP archive and streams it.
    /// ZIP structure: {StudentDisplayId}_{StudentName}/{OriginalFileName}
    /// ZIP filename: {CourseCode}_{AssignmentTitle}_{yyyy-MM-dd}.zip
    /// Returns null stream with 204 if no submissions exist.
    /// Only Tutor (assigned to the course) and Admin may call this.
    /// </summary>
    Task<(Stream? ZipStream, string ZipFileName)> DownloadAllSubmissionsAsZipAsync(
        Guid assignmentId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    // ── Review / Grading ─────────────────────────────────────────────────────

    /// <summary>
    /// Tutor/Admin grades a student's submission: sets PointsAwarded + optional Feedback.
    /// Also creates or updates the corresponding GradeRecord for the student/assignment.
    /// Only Admin or the assigned Tutor of the course may call this.
    /// </summary>
    Task<AssignmentSubmissionResponse> ReviewSubmissionAsync(
        Guid submissionId,
        ReviewSubmissionRequest request,
        Guid reviewerId,
        string reviewerRole,
        CancellationToken ct = default);
}
