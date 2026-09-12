using System.Text.Json;
using SafeRide.Ai.Application.Abstractions;

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
        var metres = ReadNumber(context.ContextJson, "MetresOffRoute");

        var draft = metres is null
            ? $"The bus on route {context.RouteCode} has left its expected route. Please check with the driver."
            : $"The bus on route {context.RouteCode} ({context.RouteName}) is about {metres:0} m from its usual route. Please check with the driver.";

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
