using SHIELDON.Domain.Entities;
using System.Security.Claims;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Contract for JWT token generation and validation.
/// Implemented in SHIELDON.Infrastructure/Services/JwtService.cs.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates a short-lived JWT Access Token containing the user's claims.
    /// Expiry: 15 minutes (configurable via JwtSettings:AccessTokenExpiryMinutes).
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a long-lived, cryptographically secure Refresh Token string.
    /// Expiry: 7 days (configurable via JwtSettings:RefreshTokenExpiryDays).
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Reads the ClaimsPrincipal from an access token WITHOUT validating expiry.
    /// Used during refresh flow to get user identity from an expired token.
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken);
}
