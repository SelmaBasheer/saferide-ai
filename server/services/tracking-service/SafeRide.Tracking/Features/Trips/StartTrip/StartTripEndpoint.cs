using System.Security.Claims;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Features.Trips.Contracts;

namespace SafeRide.Tracking.Features.Trips.StartTrip;

public static class StartTripEndpoint
{
    public static IEndpointRouteBuilder MapStartTrip(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/trips/start",
                async (
                    StartTripRequest request,
                    ClaimsPrincipal user,
                    StartTripHandler handler,
                    CancellationToken ct
                ) =>
                {
                    var trip = await handler.HandleAsync(request, user, ct);
                    return Results.Created(
                        $"/api/trips/{trip.Id}",
                        ApiResponse<TripResponse>.Ok(trip, "Trip started.")
                    );
                }
            )
            .RequireAuthorization("Driver")
            .AddEndpointFilter<ValidationFilter<StartTripRequest>>()
            .WithTags("Trips");

        return app;
    }
}
