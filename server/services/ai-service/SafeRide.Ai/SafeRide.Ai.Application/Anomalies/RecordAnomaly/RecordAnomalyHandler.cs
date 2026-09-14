using Microsoft.Extensions.Logging;
using SafeRide.Ai.Application.Abstractions;
using SafeRide.Ai.Application.Common;
using SafeRide.Ai.Domain.Entities;
using SafeRide.Ai.Domain.Enums;

namespace SafeRide.Ai.Application.Anomalies.RecordAnomaly;

public sealed class RecordAnomalyHandler(
    IAnomalyRepository anomalies,
    ILogger<RecordAnomalyHandler> logger
)
{
    public async Task<Result<Guid?>> HandleAsync(RecordAnomalyCommand command, CancellationToken ct)
    {
        // Deduplication is per trip *and per type*: a trip that both deviated and
        // skipped a stop has two genuine problems, and the school should see both.
        var existing = await anomalies.GetUnresolvedAsync(command.TripId, command.Type, ct);

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
                    "Trip {TripId} has an unclassified {Type} alert, finishing it",
                    command.TripId,
                    command.Type
                );

                return Result.Success<Guid?>(existing.Id);
            }

            logger.LogInformation(
                "Trip {TripId} already has an unresolved {Type} alert",
                command.TripId,
                command.Type
            );

            return Result.Success<Guid?>(null);
        }

        var anomaly = Anomaly.Detect(
            command.SchoolId,
            command.TripId,
            command.BusId,
            command.RouteCode,
            command.RouteName,
            command.Type,
            command.ContextJson,
            command.OccurredAtUtc
        );

        await anomalies.AddAsync(anomaly, ct);
        await anomalies.SaveChangesAsync(ct);

        logger.LogInformation(
            "Recorded {Type} anomaly {AnomalyId} for trip {TripId}",
            command.Type,
            anomaly.Id,
            command.TripId
        );

        return Result.Success<Guid?>(anomaly.Id);
    }
}
