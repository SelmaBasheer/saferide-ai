using System.Security.Claims;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Features.Trips.Contracts;

namespace SafeRide.Tracking.Features.Trips.EndTrip;

public static class EndTripEndpoint
{
    public static IEndpointRouteBuilder MapEndTrip(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/trips/{id:guid}/end",
                async (
                    Guid id,
                    ClaimsPrincipal user,
                    EndTripHandler handler,
                    CancellationToken ct
                ) =>
                {
                    var trip = await handler.HandleAsync(id, user, ct);
                    return Results.Ok(ApiResponse<TripResponse>.Ok(trip, "Trip ended."));
                }
            )
            .RequireAuthorization()
            .WithTags("Trips");

        return app;
    }
}
