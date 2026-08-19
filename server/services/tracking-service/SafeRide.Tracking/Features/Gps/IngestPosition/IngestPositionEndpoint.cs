using System.Security.Claims;
using SafeRide.Tracking.Common;
using SafeRide.Tracking.Domain;
using SafeRide.Tracking.Security;

namespace SafeRide.Tracking.Features.Gps.IngestPosition;

public sealed record IngestPositionRequest(
    double Latitude,
    double Longitude,
    double? SpeedKmh,
    string? Source
);

public static class IngestPositionEndpoint
{
    public static IEndpointRouteBuilder MapIngestPosition(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/api/trips/{id:guid}/position",
                async (
                    Guid id,
                    IngestPositionRequest request,
                    ClaimsPrincipal user,
                    IngestPositionHandler handler,
                    CancellationToken ct
                ) =>
                {
                    var source = Enum.TryParse<PositionSource>(request.Source, true, out var s)
                        ? s
                        : PositionSource.Gps;

                    await handler.HandleAsync(
                        id,
                        user.UserId(),
                        new PositionInput(
                            request.Latitude,
                            request.Longitude,
                            request.SpeedKmh,
                            source
                        ),
                        ct
                    );

                    return Results.Ok(ApiResponse<object?>.Ok(null, "Position recorded."));
                }
            )
            .RequireAuthorization("Driver")
            .WithTags("Gps");

        return app;
    }
}
