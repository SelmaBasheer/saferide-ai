using System.Text.Json;
using Microsoft.Extensions.Logging;
using SafeRide.Ai.Application.Abstractions;
using SafeRide.Ai.Application.Common;
using SafeRide.Ai.Domain.Entities;
using SafeRide.Ai.Domain.Enums;

namespace SafeRide.Ai.Application.Anomalies.RecordDeviation;

public sealed class RecordDeviationHandler(
    IAnomalyRepository anomalies,
    ILogger<RecordDeviationHandler> logger
)
{
    public async Task<Result<Guid?>> HandleAsync(
        RecordDeviationCommand command,
        CancellationToken ct
    )
    {
        // Tracking applies its own cooldown, so repeats are already rare. This
        // stops one incident becoming two alerts if one slips through.
        var existing = await anomalies.GetUnresolvedAsync(
            command.TripId,
            AnomalyType.RouteDeviation,
            ct
        );

        if (existing is not null)
        {
            // "Already recorded" and "already finished" are different things. An
            // alert still sitting at Detected never got its explanation, so hand
            // its id back and let the caller complete it. Treating it as a plain
            // duplicate would strand it forever: it can be neither approved nor
            // dismissed until it has been classified.
            if (existing.Status == AnomalyStatus.Detected)
            {
                logger.LogInformation(
                    "Trip {TripId} has an unclassified deviation alert, finishing it",
                    command.TripId
                );

                return Result.Success<Guid?>(existing.Id);
            }

            logger.LogInformation(
                "Trip {TripId} already has an unresolved deviation alert",
                command.TripId
            );

            return Result.Success<Guid?>(null);
        }

        var anomaly = Anomaly.Detect(
            command.SchoolId,
            command.TripId,
            command.BusId,
            command.RouteCode,
            command.RouteName,
            AnomalyType.RouteDeviation,
            JsonSerializer.Serialize(command),
            command.OccurredAtUtc
        );

        await anomalies.AddAsync(anomaly, ct);
        await anomalies.SaveChangesAsync(ct);

        logger.LogInformation(
            "Recorded deviation anomaly {AnomalyId} for trip {TripId}, {Metres} m off route",
            anomaly.Id,
            command.TripId,
            command.MetresOffRoute
        );

        return Result.Success<Guid?>(anomaly.Id);
    }
}
