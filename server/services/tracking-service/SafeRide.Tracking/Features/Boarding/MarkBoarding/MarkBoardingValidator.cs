using FluentValidation;

namespace SafeRide.Tracking.Features.Boarding.MarkBoarding;

public sealed class MarkBoardingValidator : AbstractValidator<MarkBoardingRequest>
{
    public MarkBoardingValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Status)
            .Must(s => s is "Boarded" or "Absent")
            .WithMessage("Status must be Boarded or Absent.");
    }
}
