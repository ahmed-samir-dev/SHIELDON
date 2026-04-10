namespace SHIELDON.Application.Features.Auth.DTOs;

/// <summary>Payload for requesting a password reset email.</summary>
public record ForgotPasswordRequest(string Email);

/// <summary>Payload for resetting a password using a secure token.</summary>
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
