using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Exams.DTOs;

namespace SHIELDON.Application.Interfaces;

public interface IExamAttemptService
{
    Task<ApiResponse<StartExamResponse>> StartExamAsync(Guid examId, Guid studentId, CancellationToken ct = default);
    Task<ApiResponse<string>> SaveAnswerAsync(Guid attemptId, Guid token, SaveAnswerRequest request, CancellationToken ct = default);
    Task<ApiResponse<SubmitExamResponse>> SubmitExamAsync(Guid attemptId, Guid token, bool isForceSubmit = false, CancellationToken ct = default);
}
