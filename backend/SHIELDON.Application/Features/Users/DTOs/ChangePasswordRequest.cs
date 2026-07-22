using FluentValidation;

namespace SHIELDON.Application.Features.Users.DTOs;

public record ChangePasswordRequest(
    string? CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword
);

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        // CurrentPassword validation is performed in ProfileService based on whether
        // the user is passwordless (Google account) or has an existing password.

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Please confirm your new password.")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}
