using FluentValidation;

namespace SafeRide.Identity.Application.Auth.Password;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Otp).NotEmpty().Matches("^[0-9]{6}$").WithMessage("OTP must be 6 digits.");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
