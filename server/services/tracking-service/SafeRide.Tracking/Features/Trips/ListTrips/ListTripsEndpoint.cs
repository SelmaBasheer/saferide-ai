using System.Security.Claims;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Features.Trips.Contracts;

namespace SafeRide.Tracking.Features.Trips.ListTrips;

public static class ListTripsEndpoint
{
    public static IEndpointRouteBuilder MapListTrips(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/api/trips",
                async (
                    string? status,
                    int? page,
                    int? pageSize,
                    ClaimsPrincipal user,
                    ListTripsHandler handler,
                    CancellationToken ct
                ) =>
                {
                    var result = await handler.HandleAsync(
                        status,
                        page ?? 1,
                        pageSize ?? 10,
                        user,
                        ct
                    );
                    return Results.Ok(ApiResponse<PagedResult<TripSummaryResponse>>.Ok(result));
                }
            )
            .RequireAuthorization()
            .WithTags("Trips");

        return app;
    }
}
