using FluentValidation;

namespace SafeRide.Tracking.Features.Trips.StartTrip;

public sealed class StartTripValidator : AbstractValidator<StartTripRequest>
{
    public StartTripValidator() => RuleFor(x => x.RouteId).NotEmpty();
}
