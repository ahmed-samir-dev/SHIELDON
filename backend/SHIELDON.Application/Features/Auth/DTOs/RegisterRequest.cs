using SHIELDON.Domain.Enums;

namespace SHIELDON.Application.Features.Auth.DTOs;

public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword,
    UserRole Role,
    string? PhoneNumber = null
);
