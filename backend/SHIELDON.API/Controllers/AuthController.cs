using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Auth.DTOs;
using SHIELDON.Application.Interfaces;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Handles all authentication-related endpoints.
/// Routes: /api/auth/*
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/auth/login
    /// Authenticates a user with email and password.
    /// Returns JWT access token + refresh token on success.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        _logger.LogInformation("User {Email} logged in successfully.", result.Email);
        return Ok(ApiResponse<LoginResponse>.Ok(result, "Login successful."));
    }

    /// <summary>
    /// POST /api/auth/refresh
    /// Rotates the refresh token and returns a new access token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        return Ok(ApiResponse<LoginResponse>.Ok(result, "Token refreshed."));
    }

    /// <summary>
    /// POST /api/auth/logout
    /// Revokes the current refresh token. Silent success — idempotent.
    /// Requires authentication (user must pass a valid access token).
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<object>.Fail("Invalid session."));

        await _authService.LogoutAsync(userId, request.RefreshToken, cancellationToken);
        _logger.LogInformation("User {UserId} logged out.", userId);
        return Ok(ApiResponse<object>.Ok(null, "Logged out successfully."));
    }

    /// <summary>
    /// POST /api/auth/verify-email
    /// Verifies a user's email address using the token from their verification link.
    /// No authentication required — this is called from the email link.
    /// </summary>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.VerifyEmailAsync(request, cancellationToken);
        _logger.LogInformation("Email verified for {Email}.", request.Email);
        return Ok(ApiResponse<object>.Ok(null, "Email verified successfully. You can now log in."));
    }

    /// <summary>
    /// POST /api/auth/resend-verification
    /// Re-sends the verification email for an unverified account.
    /// Rate-limited at the service layer.
    /// </summary>
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ResendVerificationEmailAsync(request, cancellationToken);
        // Always return the same message to prevent email enumeration
        return Ok(ApiResponse<object>.Ok(null,
            "If this email is registered and unverified, a new verification email has been sent."));
    }
}

/// <summary>Simple request body for token rotation and logout.</summary>
public record RefreshTokenRequest(string RefreshToken);


