using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Exams.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Manages the full exam lifecycle: CRUD, publishing, and notification dispatch.
/// Implemented by ExamService in the Infrastructure layer.
/// </summary>
public interface IExamService
{
    /// <summary>Tutor/Admin creates a new draft exam for a course.</summary>
    Task<ExamSummaryResponse> CreateExamAsync(Guid courseId, CreateExamRequest request, Guid requestingUserId, string requestingUserRole, CancellationToken ct = default);

    /// <summary>Returns a paginated list of exams for a course. Role-filtered.</summary>
    Task<PagedResponse<ExamSummaryResponse>> GetExamsAsync(Guid courseId, ExamQueryParams query, Guid requestingUserId, string requestingUserRole, CancellationToken ct = default);

    /// <summary>Returns full detail of a single exam.</summary>
    Task<ExamDetailResponse> GetExamByIdAsync(Guid examId, Guid requestingUserId, string requestingUserRole, CancellationToken ct = default);

    /// <summary>Tutor/Admin updates an existing exam. Only Draft exams can be fully edited.</summary>
    Task<ExamDetailResponse> UpdateExamAsync(Guid examId, UpdateExamRequest request, Guid requestingUserId, string requestingUserRole, CancellationToken ct = default);

    /// <summary>Deletes a Draft exam. Published/Closed exams cannot be deleted.</summary>
    Task DeleteExamAsync(Guid examId, Guid requestingUserId, string requestingUserRole, CancellationToken ct = default);

    /// <summary>
    /// Publishes an exam (Draft → Published).
    /// Triggers in-app + email notification to all enrolled students.
    /// </summary>
    Task<ExamDetailResponse> PublishExamAsync(Guid examId, Guid requestingUserId, string requestingUserRole, CancellationToken ct = default);
}
