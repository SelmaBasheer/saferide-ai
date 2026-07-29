using FluentValidation;

namespace SafeRide.Identity.Application.Auth.Password;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Otp).NotEmpty().Length(6);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
