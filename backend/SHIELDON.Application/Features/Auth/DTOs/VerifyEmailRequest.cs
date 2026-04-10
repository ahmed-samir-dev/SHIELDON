namespace SHIELDON.Application.Features.Auth.DTOs;

/// <summary>Payload for verifying an account via the email link token.</summary>
public record VerifyEmailRequest(string Email, string Token);

/// <summary>Payload for requesting a new verification email.</summary>
public record ResendVerificationRequest(string Email);
