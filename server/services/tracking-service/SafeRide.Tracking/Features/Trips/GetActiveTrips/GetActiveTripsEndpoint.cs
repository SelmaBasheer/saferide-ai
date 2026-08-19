using System.Security.Claims;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Features.Trips.Contracts;

namespace SafeRide.Tracking.Features.Trips.EndTrip.GetActiveTrips;

public static class GetActiveTripsEndpoint
{
    public static IEndpointRouteBuilder MapGetActiveTrips(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/trips/active",
                async (ClaimsPrincipal user, GetActiveTripsHandler handler, CancellationToken ct) =>
                {
                    var trips = await handler.HandleAsync(user, ct);
                    return Results.Ok(ApiResponse<List<TripSummaryResponse>>.Ok(trips));
                }
            )
            .RequireAuthorization("SchoolAdmin")
            .WithTags("Trips");

        return app;
    }
}
