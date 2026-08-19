using System.Security.Claims;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Features.Trips.Contracts;

namespace SafeRide.Tracking.Features.Boarding.MarkBoarding;

public static class MarkBoardingEndpoint
{
    public static IEndpointRouteBuilder MapMarkBoarding(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/trips/{id:guid}/boarding",
                async (
                    Guid id,
                    MarkBoardingRequest request,
                    ClaimsPrincipal user,
                    MarkBoardingHandler handler,
                    CancellationToken ct
                ) =>
                {
                    var trip = await handler.HandleAsync(id, request, user, ct);
                    return Results.Ok(ApiResponse<TripResponse>.Ok(trip, "Boarding recorded."));
                }
            )
            .RequireAuthorization("Driver")
            .AddEndpointFilter<ValidationFilter<MarkBoardingRequest>>()
            .WithTags("Boarding");

        return app;
    }
}
