using System.Security.Claims;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Features.Trips.Contracts;

namespace SafeRide.Tracking.Features.Trips.GetTrip;

public static class GetTripEndpoint
{
    public static IEndpointRouteBuilder MapGetTrip(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/trips/{id:guid}",
                async (
                    Guid id,
                    ClaimsPrincipal user,
                    GetTripHandler handler,
                    CancellationToken ct
                ) =>
                {
                    var trip = await handler.HandleAsync(id, user, ct);
                    return Results.Ok(ApiResponse<TripResponse>.Ok(trip));
                }
            )
            .RequireAuthorization()
            .WithTags("Trips");

        return app;
    }
}
