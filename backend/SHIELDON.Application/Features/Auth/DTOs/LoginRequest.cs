namespace SHIELDON.Application.Features.Auth.DTOs;

/// <summary>
/// The request body for the POST /api/auth/login endpoint.
/// </summary>
public record LoginRequest(
    string Email,
    string Password
);
