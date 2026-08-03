using FluentValidation;

namespace SafeRide.Identity.Application.Auth.Verify;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Otp).NotEmpty().Matches("^[0-9]{6}$").WithMessage("OTP must be 6 digits.");
    }
}
