using SHIELDON.Domain.Enums;

namespace SHIELDON.Application.Features.Auth.DTOs;

/// <summary>
/// Request DTO for Google Sign-In authentication.
/// Contains the Google ID token (credential) and optional Role for new registrations.
/// </summary>
public record GoogleAuthRequest(
    string IdToken,
    string? Role = null
);
