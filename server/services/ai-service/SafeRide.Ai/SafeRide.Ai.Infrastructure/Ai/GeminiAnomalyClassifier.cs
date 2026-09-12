using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SafeRide.Ai.Application.Abstractions;

namespace SafeRide.Ai.Infrastructure.Ai;

public sealed class GeminiAnomalyClassifier(
    HttpClient http,
    IOptions<GeminiSettings> options,
    TemplateAnomalyClassifier fallback,
    ILogger<GeminiAnomalyClassifier> logger
) : IAnomalyClassifier
{
    private const string SystemPrompt = """
        You classify anomalies detected on school bus trips in Kerala, India.

        You receive a JSON context describing what the monitoring system observed.
        Decide the single most likely explanation and write a short note for the
        school office.

        Guidance:
        - A very large distance off route, more than 5 km, usually means a GPS
          glitch or a test position rather than a real detour.
        - Low speed with a small deviation usually means traffic.
        - Near-zero speed with a deviation may mean a breakdown.
        - Never state a cause as certain, never promise a time, never name a child.
        - The message is for a school office clerk, not an engineer. Two sentences,
          plain language, no jargon.
        """;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// Gemini enforces this shape on the response, so the reply is valid JSON by
    /// construction rather than by asking the model nicely.
    private static readonly object ResponseSchema = new
    {
        type = "OBJECT",
        properties = new
        {
            label = new
            {
                type = "STRING",
                @enum = new[] { "Traffic", "Breakdown", "UnauthorisedDetour", "DataError" },
            },
            confidence = new { type = "NUMBER" },
            reasoning = new { type = "STRING" },
            message = new { type = "STRING" },
        },
        required = new[] { "label", "confidence", "reasoning", "message" },
    };

    private readonly GeminiSettings _settings = options.Value;

    public async Task<Classification> ClassifyAsync(
        AnomalyContext context,
        CancellationToken ct = default
    )
    {
        try
        {
            var request = new
            {
                system_instruction = new { parts = new[] { new { text = SystemPrompt } } },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new
                            {
                                text = $"Anomaly type: {context.Type}\nRoute: {context.RouteCode} — {context.RouteName}\n\nContext:\n{context.ContextJson}",
                            },
                        },
                    },
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseSchema = ResponseSchema,
                    maxOutputTokens = _settings.MaxTokens,
                },
            };

            // Absolute URL on purpose: combining a relative path with BaseAddress can
            // percent-encode the colon in ":generateContent", which Google 404s on.
            var url =
                $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.Model}:generateContent";

            var response = await http.PostAsJsonAsync(url, request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);

                logger.LogError(
                    "Gemini returned {Status}: {Body}",
                    (int)response.StatusCode,
                    error
                );

                return await fallback.ClassifyAsync(context, ct);
            }

            var body = await response.Content.ReadFromJsonAsync<GeminiResponse>(Json, ct);

            var candidate = body?.Candidates?.FirstOrDefault();
            var text = candidate?.Content?.Parts?.FirstOrDefault()?.Text;

            if (candidate?.FinishReason is not null and not "STOP")
            {
                logger.LogWarning(
                    "Model stopped early: {FinishReason}. Raise MaxTokens if this is MAX_TOKENS.",
                    candidate.FinishReason
                );
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                logger.LogWarning("Model returned no text, falling back to template");
                return await fallback.ClassifyAsync(context, ct);
            }

            ModelVerdict? verdict;

            try
            {
                verdict = JsonSerializer.Deserialize<ModelVerdict>(text, Json);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Model reply was not valid JSON: {Text}", text);
                return await fallback.ClassifyAsync(context, ct);
            }
            if (verdict is null || string.IsNullOrWhiteSpace(verdict.Message))
            {
                logger.LogWarning("Model reply was not usable, falling back to template");
                return await fallback.ClassifyAsync(context, ct);
            }

            logger.LogInformation(
                "Model classified anomaly as {Label} with confidence {Confidence}",
                verdict.Label,
                verdict.Confidence
            );

            return new Classification(
                Label: verdict.Label ?? "Unclassified",
                Reasoning: verdict.Reasoning ?? string.Empty,
                DraftMessage: verdict.Message,
                Confidence: verdict.Confidence,
                ByModel: true
            );
        }
        catch (Exception ex)
        {
            // An alert that arrives plainly worded beats one that never arrives
            // because an external API was unavailable.
            logger.LogError(ex, "Model classification failed, falling back to template");
            return await fallback.ClassifyAsync(context, ct);
        }
    }

    private sealed record GeminiResponse(List<GeminiCandidate>? Candidates);

    private sealed record GeminiCandidate(GeminiContent? Content, string? FinishReason);

    private sealed record GeminiContent(List<GeminiPart>? Parts);

    private sealed record GeminiPart(string? Text);

    private sealed record ModelVerdict(
        string? Label,
        double? Confidence,
        string? Reasoning,
        string? Message
    );
}
