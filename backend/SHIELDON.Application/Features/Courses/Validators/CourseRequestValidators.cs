using FluentValidation;
using SHIELDON.Application.Features.Courses.DTOs;

namespace SHIELDON.Application.Features.Courses.Validators;

public class CreateCourseRequestValidator : AbstractValidator<CreateCourseRequest>
{
    public CreateCourseRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Course title is required.")
            .MaximumLength(200).WithMessage("Course title must not exceed 200 characters.");

        RuleFor(x => x.CourseCode)
            .NotEmpty().WithMessage("Course code is required.")
            .MaximumLength(20).WithMessage("Course code must not exceed 20 characters.")
            .Matches(@"^[A-Za-z0-9\-]+$").WithMessage("Course code may only contain letters, numbers, and hyphens.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description is not null);
    }
}

public class UpdateCourseRequestValidator : AbstractValidator<UpdateCourseRequest>
{
    public UpdateCourseRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Course title is required.")
            .MaximumLength(200).WithMessage("Course title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description is not null);
    }
}

public class ReviewEnrollmentRequestValidator : AbstractValidator<ReviewEnrollmentRequest>
{
    public ReviewEnrollmentRequestValidator()
    {
        // If rejecting, a reason is strongly encouraged but not mandatory
        RuleFor(x => x.RejectionReason)
            .MaximumLength(500).WithMessage("Rejection reason must not exceed 500 characters.")
            .When(x => !x.Approved && x.RejectionReason is not null);
    }
}

public class BulkReviewEnrollmentRequestValidator : AbstractValidator<BulkReviewEnrollmentRequest>
{
    public BulkReviewEnrollmentRequestValidator()
    {
        RuleFor(x => x.EnrollmentIds)
            .NotEmpty().WithMessage("At least one enrollment ID must be provided.")
            .Must(ids => ids.Count <= 100).WithMessage("Cannot bulk-review more than 100 enrollments at once.");

        RuleFor(x => x.RejectionReason)
            .MaximumLength(500).WithMessage("Rejection reason must not exceed 500 characters.")
            .When(x => !x.Approved && x.RejectionReason is not null);
    }
}
