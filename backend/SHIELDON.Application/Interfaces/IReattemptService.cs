using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Exams.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Manages the re-attempt request lifecycle:
///   - Student submits a request after exhausting attempts
///   - Admin/Tutor reviews (approve/reject) the request
///   - Notifications dispatched on status changes
/// </summary>
public interface IReattemptService
{
    /// <summary>
    /// Student submits a re-attempt request for a specific exam.
    /// Rules:
    ///   - Student must be enrolled in the exam's course
    ///   - Exam must be Published
    ///   - Student must have exhausted all MaxAttempts (attempt count >= MaxAttempts)
    ///   - No existing Pending request for the same exam
    /// </summary>
    Task<StudentReattemptStatusResponse> SubmitRequestAsync(
        Guid examId, Guid studentId, SubmitReattemptRequest request, CancellationToken ct = default);

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
    /// Admin/Tutor approves or rejects a pending re-attempt request.
    ///   - Only Pending requests can be reviewed
    ///   - Approval resets the student's attempt count eligibility (+1 extra attempt)
    ///   - In-app + email notification dispatched to the student
    /// </summary>
    Task<ReattemptRequestResponse> ReviewRequestAsync(
        Guid requestId, Guid reviewerId, string reviewerRole, ReviewReattemptRequest request, CancellationToken ct = default);
}
