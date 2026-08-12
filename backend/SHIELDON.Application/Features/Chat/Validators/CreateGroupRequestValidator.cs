using FluentValidation;
using SHIELDON.Application.Features.Chat.DTOs;

namespace SHIELDON.Application.Features.Chat.Validators;

/// <summary>
/// Validates the CreateGroupRequest payload.
/// Only Admin and Tutor roles are authorized to call the group creation endpoint -
/// role enforcement is handled separately via [Authorize] in the controller.
/// </summary>
public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(x => x.GroupName)
            .NotEmpty().WithMessage("Group name is required.")
            .MaximumLength(100).WithMessage("Group name cannot exceed 100 characters.");

        RuleFor(x => x.MemberIds)
            .NotNull().WithMessage("Member list cannot be null.")
            .Must(ids => ids.Count >= 1)
            .WithMessage("A group must have at least 1 member besides the creator.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate member IDs are not allowed.");
    }
}
