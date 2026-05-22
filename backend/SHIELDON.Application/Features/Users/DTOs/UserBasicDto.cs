namespace SHIELDON.Application.Features.Users.DTOs;

public record UserBasicDto(
    Guid Id,
    string FullName,
    string Email,
    string Role
);
