using FluentValidation;

namespace SHIELDON.Application.Features.Users.DTOs;

/// <summary>Request DTO to save or update user phone number.</summary>
public record UpdatePhoneRequest(string PhoneNumber);

/// <summary>Request DTO to trigger OTP via WhatsApp.</summary>
public record SendPhoneOtpRequest(string Channel = "whatsapp");

/// <summary>Request DTO to verify 6-digit OTP code.</summary>
public record VerifyPhoneOtpRequest(string Code);

// ── FluentValidation Validators ──────────────────────────────────────────────

public class UpdatePhoneRequestValidator : AbstractValidator<UpdatePhoneRequest>
{
    public UpdatePhoneRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\+[1-9]\d{6,14}$")
            .WithMessage("Phone number must be in valid E.164 international format (e.g. +201012345678).");
    }
}

public class SendPhoneOtpRequestValidator : AbstractValidator<SendPhoneOtpRequest>
{
    public SendPhoneOtpRequestValidator()
    {
        RuleFor(x => x.Channel)
            .NotEmpty().WithMessage("Verification channel is required.")
            .Must(c => c != null && c.ToLower() == "whatsapp")
            .WithMessage("Channel must be 'whatsapp'.");
    }
}

public class VerifyPhoneOtpRequestValidator : AbstractValidator<VerifyPhoneOtpRequest>
{
    public VerifyPhoneOtpRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("OTP verification code is required.")
            .Matches(@"^\d{6}$")
            .WithMessage("Verification code must be exactly 6 digits.");
    }
}
