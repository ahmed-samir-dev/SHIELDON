using SHIELDON.Domain.Enums;

namespace SHIELDON.Application.Features.Users.DTOs;

public record UserProfileResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? ProfilePictureUrl,
    UserRole Role,
    string? DisplayId,
    AccountStatus AccountStatus,
    DateTime CreatedAt,
    bool HasPassword = true,
    string? PhoneNumber = null,
    string PhoneVerificationStatus = "None",
    DateTime? PhoneVerifiedAt = null
);

public record UpdateProfileRequest(
    string FirstName,
    string LastName
);
