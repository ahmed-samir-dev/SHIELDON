using SHIELDON.Application.Features.Users.DTOs;

namespace SHIELDON.Application.Interfaces;

public interface IProfileService
{
    Task<UserProfileResponse> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<UserProfileResponse> UploadProfilePictureAsync(Guid userId, Stream fileStream, string contentType, string originalFileName, CancellationToken ct = default);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task CompleteOnboardingAsync(Guid userId, CancellationToken ct = default);
    Task ResetOnboardingAsync(Guid userId, CancellationToken ct = default);
    Task<UserProfileResponse> UpdatePhoneAsync(Guid userId, UpdatePhoneRequest request, CancellationToken ct = default);
    Task SendPhoneOtpAsync(Guid userId, SendPhoneOtpRequest request, CancellationToken ct = default);
    Task<UserProfileResponse> VerifyPhoneOtpAsync(Guid userId, VerifyPhoneOtpRequest request, CancellationToken ct = default);
}
