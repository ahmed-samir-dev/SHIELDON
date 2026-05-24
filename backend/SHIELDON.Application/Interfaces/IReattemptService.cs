using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Exams.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Manages the request lifecycle for:
///   1. Re-attempt requests: student had a failed/expired attempt and wants another try.
///   2. Re-open requests: student never entered the exam (0 attempts) and the exam has expired.
///      On approval, an ExamExtension row is created granting that student a personal deadline.
/// </summary>
public interface IReattemptService
{
    /// <summary>
    /// Student submits a re-attempt or re-open request for a specific exam.
    /// Rules for Re-attempt (IsReopenRequest = false):
    ///   - Student must have exhausted all MaxAttempts
    ///   - No existing Pending request for the same exam
    /// Rules for Re-open (IsReopenRequest = true):
    ///   - Student must have 0 attempts on the exam
    ///   - Exam EndTime must be in the past (expired)
    ///   - No existing Pending request for the same exam
    /// attachmentFile is optional proof (max 10 MB: .jpg, .jpeg, .png, .pdf, .docx).
    /// </summary>
    Task<StudentReattemptStatusResponse> SubmitRequestAsync(
        Guid examId,
        Guid studentId,
        SubmitReattemptRequest request,
        Stream? attachmentStream,
        string? attachmentFileName,
        long attachmentSize,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all re-attempt requests visible to the requesting user.
    ///   - Admin: sees all requests
    ///   - Tutor: sees requests for exams in their courses
    ///   - Student: sees only their own requests
    /// </summary>
    Task<PagedResponse<ReattemptRequestResponse>> GetRequestsAsync(
        ReattemptQueryParams query, Guid requestingUserId, string requestingUserRole, CancellationToken ct = default);

    /// <summary>Returns all re-attempt requests submitted by the student.</summary>
    Task<IReadOnlyList<StudentReattemptStatusResponse>> GetMyRequestsAsync(
        Guid studentId, CancellationToken ct = default);

    /// <summary>
    /// Admin/Tutor approves or rejects a pending request.
    ///   - Approval of Re-attempt: resets attempt eligibility (+1 extra attempt).
    ///   - Approval of Re-open: creates an ExamExtension row granting that student
    ///     a personal window of ExtensionHours (24 or 48) from now to take the exam.
    ///   - In-app + email notification dispatched to the student on either decision.
    /// </summary>
    Task<ReattemptRequestResponse> ReviewRequestAsync(
        Guid requestId, Guid reviewerId, string reviewerRole, ReviewReattemptRequest request, CancellationToken ct = default);

    /// <summary>
    /// Returns all eligible students who can submit a Re-open request for a given exam.
    /// (students enrolled in the course, 0 attempts, exam expired)
    /// Used by the frontend to decide whether to show the Re-open button.
    /// </summary>
    Task<bool> CanStudentSubmitReopenRequestAsync(Guid examId, Guid studentId, CancellationToken ct = default);
}
