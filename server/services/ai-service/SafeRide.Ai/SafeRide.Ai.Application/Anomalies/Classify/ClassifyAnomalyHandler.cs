using Microsoft.Extensions.Logging;
using SafeRide.Ai.Application.Abstractions;
using SafeRide.Ai.Application.Common;

namespace SafeRide.Ai.Application.Anomalies.Classify;

public sealed class ClassifyAnomalyHandler(
    IAnomalyRepository anomalies,
    IAnomalyClassifier classifier,
    ILogger<ClassifyAnomalyHandler> logger
)
{
    public async Task<Result> HandleAsync(Guid anomalyId, CancellationToken ct)
    {
        var anomaly = await anomalies.GetAsync(anomalyId, ct);

        if (anomaly is null)
        {
            return Result.Failure(AnomalyErrors.NotFound);
        }

        var classification = await classifier.ClassifyAsync(
            new AnomalyContext(
                anomaly.Type,
                anomaly.RouteCode,
                anomaly.RouteName,
                anomaly.ContextJson
            ),
            ct
        );

        if (
            !anomaly.TryClassify(
                classification.Label,
                classification.Reasoning,
                classification.DraftMessage,
                classification.Confidence,
                classification.ByModel
            )
        )
        {
            return Result.Failure(AnomalyErrors.AlreadyResolved);
        }

        await anomalies.SaveChangesAsync(ct);

        logger.LogInformation(
            "Classified anomaly {AnomalyId} as {Label}, by model: {ByModel}",
            anomaly.Id,
            classification.Label,
            classification.ByModel
        );

        return Result.Success();
    }
}
