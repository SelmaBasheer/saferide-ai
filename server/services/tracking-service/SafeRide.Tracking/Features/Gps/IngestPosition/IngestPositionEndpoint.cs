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
                    PositionSource source;
                    if (string.IsNullOrWhiteSpace(request.Source))
                    {
                        source = PositionSource.Gps;
                    }
                    else if (!Enum.TryParse(request.Source, true, out source))
                    {
                        throw AppException.Validation(
                            $"Unknown position source '{request.Source}'."
                        );
                    }

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
