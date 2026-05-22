using SHIELDON.Application.Features.Auth.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Authentication service contract.
/// Handles login, token refresh, logout, and email verification operations.
/// Lives in the Application layer - no dependency on ASP.NET Core.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new Student or Tutor account. Sets AccountStatus to Unverified and sends verification email.
    /// Throws DomainException if email is already in use or role is invalid.
    /// </summary>
    Task RegisterAsync(RegisterRequest request, CancellationToken ct = default);

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
    /// Revokes the given refresh token for the specified user - used on logout.
    /// </summary>
    Task LogoutAsync(Guid userId, string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Verifies a user's email address using the token from their verification email.
    /// Sets AccountStatus to Active on success.
    /// Throws DomainException if token is invalid, expired, or already used.
    /// </summary>
    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default);

    /// <summary>
    /// Re-sends the verification email for an unverified account.
    /// Rate-limited: only sends if no active token exists within the last 2 minutes.
    /// Throws DomainException if the account is already verified or does not exist.
    /// </summary>
    Task ResendVerificationEmailAsync(ResendVerificationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Initiates the password reset flow. Generates a secure token and sends an email.
    /// Silently succeeds if the email doesn't exist, to prevent email enumeration.
    /// Rate-limited: ignores consecutive requests within a short timeframe.
    /// </summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);

    /// <summary>
    /// Validates the secure token and updates the user's password.
    /// Unlocks the account and resets failed login attempts upon success.
    /// Throws DomainException if the token is invalid or expired.
    /// </summary>
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}

