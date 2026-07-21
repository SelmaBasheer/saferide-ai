using FluentValidation;

namespace SafeRide.Identity.Application.Auth.Register;

public sealed class RegisterSchoolAdminCommandValidator
    : AbstractValidator<RegisterSchoolAdminCommand>
{
    public RegisterSchoolAdminCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.SchoolName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SchoolAddress).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.District).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty();
        RuleFor(x => x.Pincode).NotEmpty();
    }
}
