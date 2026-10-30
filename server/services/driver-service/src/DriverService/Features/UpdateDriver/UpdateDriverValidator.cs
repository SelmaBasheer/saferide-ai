using FluentValidation;

namespace DriverService.Features.UpdateDriver;

public sealed class UpdateDriverValidator : AbstractValidator<UpdateDriverRequest>
{
    public UpdateDriverValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(60);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(60);

        RuleFor(x => x.Phone)
            .NotEmpty()
            .Matches(@"^\+?[0-9\s-]{8,16}$")
            .WithMessage("Enter a valid phone number.");

        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(32);

        // No minimum: an admin must be able to record a licence that has
        // already expired. The consequences are derived, not prevented here.
        RuleFor(x => x.LicenseExpiryDate)
            .GreaterThan(new DateOnly(2000, 1, 1))
            .WithMessage("Enter a valid expiry date.");
    }
}
