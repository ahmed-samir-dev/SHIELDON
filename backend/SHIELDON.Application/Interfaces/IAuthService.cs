using SHIELDON.Application.Features.Auth.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Authentication service contract.
/// Handles login, token refresh, and logout operations.
/// Lives in the Application layer — no dependency on ASP.NET Core.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Validates credentials and returns JWT tokens on success.
    /// Throws DomainException on invalid credentials, unverified or locked accounts.
    /// </summary>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>
    /// Rotates the refresh token: validates the old one, revokes it, and issues a new pair.
    /// Throws DomainException if the refresh token is invalid, expired, or revoked.
    /// </summary>
    Task<LoginResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Revokes the given refresh token for the specified user — used on logout.
    /// </summary>
    Task LogoutAsync(Guid userId, string refreshToken, CancellationToken ct = default);
}
