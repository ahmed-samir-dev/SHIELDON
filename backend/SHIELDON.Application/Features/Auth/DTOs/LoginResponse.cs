using SHIELDON.Domain.Enums;

namespace SHIELDON.Application.Features.Auth.DTOs;

/// <summary>
/// The response body for a successful login.
/// Contains tokens and the minimal user info needed by the frontend.
/// </summary>
public record LoginResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string? ProfilePictureUrl,
    UserRole Role,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    bool HasCompletedOnboarding
);
