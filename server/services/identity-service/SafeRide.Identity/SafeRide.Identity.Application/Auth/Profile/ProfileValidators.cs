using FluentValidation;

namespace SafeRide.Identity.Application.Auth.Profile;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(75);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(75);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
    }
}

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Must((cmd, next) => next != cmd.CurrentPassword)
            .WithMessage("Your new password must be different from your current one.");
    }
}
