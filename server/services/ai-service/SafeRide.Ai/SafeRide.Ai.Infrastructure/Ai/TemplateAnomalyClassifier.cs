using System.Text.Json;
using SafeRide.Ai.Application.Abstractions;
using SafeRide.Ai.Domain.Enums;

namespace SafeRide.Ai.Infrastructure.Ai;

/// Used when no model is configured, or when the model call fails. Produces a
/// plain factual message rather than an interpretation — the alert still reaches
/// the school, it just isn't explained.
public sealed class TemplateAnomalyClassifier : IAnomalyClassifier
{
    public Task<Classification> ClassifyAsync(
        AnomalyContext context,
        CancellationToken ct = default
    )
    {
        var draft = context.Type switch
        {
            AnomalyType.RouteDeviation => Deviation(context),
            AnomalyType.SkippedStop => SkippedStops(context),

            // A detector exists that this classifier has not been taught about.
            // Vague beats wrong: the office still learns something happened.
            _ =>
                $"An unusual event was recorded on route {context.RouteCode} ({context.RouteName}). Please check with the driver.",
        };

        return Task.FromResult(
            new Classification(
                Label: "Unclassified",
                Reasoning: "No interpretation available — this alert was not classified by a model.",
                DraftMessage: draft,
                Confidence: null,
                ByModel: false
            )
        );
    }

    private static string Deviation(AnomalyContext context)
    {
        var metres = ReadNumber(context.ContextJson, "MetresOffRoute");

        return metres is null
            ? $"The bus on route {context.RouteCode} has left its expected route. Please check with the driver."
            : $"The bus on route {context.RouteCode} ({context.RouteName}) is about {metres:0} m from its usual route. Please check with the driver.";
    }

    private static string SkippedStops(AnomalyContext context)
    {
        var total = ReadNumber(context.ContextJson, "StopsTotal");
        var reached = ReadNumber(context.ContextJson, "StopsReached");

        return total is null || reached is null
            ? $"The bus on route {context.RouteCode} finished its trip without reaching every stop. Please check with the driver."
            : $"The bus on route {context.RouteCode} ({context.RouteName}) finished its trip having reached {reached:0} of {total:0} stops. Please check with the driver.";
    }

    private static double? ReadNumber(string json, string property)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return
                document.RootElement.TryGetProperty(property, out var value)
                && value.TryGetDouble(out var number)
                ? number
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
