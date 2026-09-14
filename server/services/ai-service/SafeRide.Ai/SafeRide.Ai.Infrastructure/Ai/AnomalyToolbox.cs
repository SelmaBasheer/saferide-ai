using System.Text.Json;
using Microsoft.Extensions.Logging;
using SafeRide.Ai.Application.Abstractions;

namespace SafeRide.Ai.Infrastructure.Ai;

/// The questions the model is allowed to ask before deciding. Every one is
/// answered from the AI service's own database, so a tool call cannot fail
/// because another service is deploying — and there is no service-to-service
/// authentication to get wrong.
public sealed class AnomalyToolbox(IAnomalyInsights insights, ILogger<AnomalyToolbox> logger)
{
    public const string RouteHistory = "get_route_history";
    public const string BusHistory = "get_bus_history";
    public const string TripAnomalies = "get_trip_anomalies";

    private const int DefaultDays = 30;

    /// Note what is *absent* from these parameters: no route code, no bus id, no
    /// school id. The subjects come from the anomaly being classified, so the
    /// model can only ask about this incident. It has no vocabulary for asking
    /// about another school's buses.
    public static readonly object Declarations = new
    {
        functionDeclarations = new object[]
        {
            new
            {
                name = RouteHistory,
                description = "Past anomalies recorded on this same route. Use it to tell a one-off from "
                    + "a pattern: a route that deviates repeatedly usually has an out-of-date "
                    + "stored path rather than a misbehaving driver.",
                parameters = new
                {
                    type = "OBJECT",
                    properties = new
                    {
                        days = new
                        {
                            type = "INTEGER",
                            description = "How many days back to look, between 1 and 90.",
                        },
                    },
                    required = new[] { "days" },
                },
            },
            new
            {
                name = BusHistory,
                description = "Past anomalies recorded for this same bus across all of its routes. Use "
                    + "it to tell a vehicle problem from a route problem.",
                parameters = new
                {
                    type = "OBJECT",
                    properties = new
                    {
                        days = new
                        {
                            type = "INTEGER",
                            description = "How many days back to look, between 1 and 90.",
                        },
                    },
                    required = new[] { "days" },
                },
            },
            new
            {
                name = TripAnomalies,
                description = "Anomalies already recorded on this trip. Use it to see whether this is "
                    + "the first sign of trouble or the latest of several.",
                parameters = new { type = "OBJECT", properties = new { } },
            },
        },
    };

    public async Task<object> InvokeAsync(
        string name,
        JsonElement arguments,
        AnomalyContext context,
        CancellationToken ct
    )
    {
        var days = ReadDays(arguments);

        AnomalyHistory? history = null;

        switch (name)
        {
            case RouteHistory:
                history = await insights.ForRouteAsync(
                    context.SchoolId,
                    context.RouteCode,
                    days,
                    ct
                );
                break;

            case BusHistory:
                history = await insights.ForBusAsync(context.SchoolId, context.BusId, days, ct);
                break;

            case TripAnomalies:
                history = await insights.ForTripAsync(context.SchoolId, context.TripId, ct);
                break;
        }

        if (history is null)
        {
            // An invented tool name is the model's mistake, not a crash. Handing
            // the mistake back lets it correct itself on the next turn, which is
            // cheaper than abandoning the whole classification.
            logger.LogWarning("Model asked for unknown tool {Tool}", name);
            return new { error = $"There is no tool called {name}." };
        }

        logger.LogInformation(
            "Tool {Tool} answered: {Total} past anomalies over {Days} days",
            name,
            history.Total,
            days
        );

        return new
        {
            total = history.Total,
            byType = history.ByType,
            byClassification = history.ByClassification,
        };
    }

    /// A missing or unreadable argument is not worth a failed turn. Thirty days
    /// is a sensible question to have asked.
    private static int ReadDays(JsonElement arguments) =>
        arguments.ValueKind == JsonValueKind.Object
        && arguments.TryGetProperty("days", out var value)
        && value.TryGetInt32(out var days)
            ? days
            : DefaultDays;
}
