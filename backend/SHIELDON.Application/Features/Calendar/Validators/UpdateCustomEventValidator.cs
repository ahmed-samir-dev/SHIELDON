using FluentValidation;
using SHIELDON.Application.Features.Calendar.DTOs;

namespace SHIELDON.Application.Features.Calendar.Validators;

public class UpdateCustomEventValidator : AbstractValidator<UpdateCustomEventRequest>
{
    public UpdateCustomEventValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

        RuleFor(x => x.EventDate)
            .NotEmpty().WithMessage("Event Date is required.");

        RuleFor(x => x.EventEndDate)
            .GreaterThanOrEqualTo(x => x.EventDate)
            .When(x => x.EventEndDate.HasValue)
            .WithMessage("End date must be on or after the start date.");
    }
}
