using System.Net;
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

    /// Two attempts, not three. Each one can burn the full HTTP timeout, so the
    /// consumer is already waiting; a third attempt buys little and costs a lot.
    private const int MaxAttempts = 2;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

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
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var classification = await AskModelAsync(context, ct);

                // Null means the model answered, but unusably — a bad key, a
                // malformed reply. Asking again would produce the same thing, so
                // stop and use the template.
                if (classification is null)
                {
                    break;
                }

                return classification;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // The service is shutting down. This is not a Gemini problem, and
                // it must not be mistaken for one.
                throw;
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < MaxAttempts)
            {
                // A timeout arrives here rather than above: HttpClient cancels the
                // request with its own token, so ct is still perfectly healthy.
                logger.LogWarning(
                    ex,
                    "Gemini attempt {Attempt} of {Max} failed, retrying in {Seconds}s",
                    attempt,
                    MaxAttempts,
                    RetryDelay.TotalSeconds
                );

                await Task.Delay(RetryDelay, ct);
            }
            catch (Exception ex)
            {
                // An alert that arrives plainly worded beats one that never arrives
                // because an external API was unavailable.
                logger.LogError(ex, "Model classification failed, falling back to template");
                break;
            }
        }

        return await fallback.ClassifyAsync(context, ct);
    }

    /// One attempt. Returns null when the failure is permanent, and throws when it
    /// is worth trying again — so the caller above never has to interpret an
    /// HTTP status code itself.
    private async Task<Classification?> AskModelAsync(AnomalyContext context, CancellationToken ct)
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

            logger.LogError("Gemini returned {Status}: {Body}", (int)response.StatusCode, error);

            // 429 means "you are going too fast" and 5xx means "we are struggling".
            // Google says so itself: spikes in demand are usually temporary. Both
            // are worth another attempt. A 400 or a 403 is our mistake and never
            // will be.
            if (IsTransientStatus(response.StatusCode))
            {
                throw new TransientModelException($"Gemini returned {(int)response.StatusCode}.");
            }

            return null;
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
            return null;
        }

        ModelVerdict? verdict;

        try
        {
            verdict = JsonSerializer.Deserialize<ModelVerdict>(text, Json);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Model reply was not valid JSON: {Text}", text);
            return null;
        }

        if (verdict is null || string.IsNullOrWhiteSpace(verdict.Message))
        {
            logger.LogWarning("Model reply was not usable, falling back to template");
            return null;
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

    private static bool IsTransientStatus(HttpStatusCode status) =>
        status == HttpStatusCode.TooManyRequests || (int)status >= 500;

    private static bool IsTransient(Exception ex) =>
        ex is TransientModelException or HttpRequestException or TaskCanceledException;

    /// Signals "worth another attempt". A separate type so the retry decision is
    /// made once, where the status code is still in view.
    private sealed class TransientModelException(string message) : Exception(message);

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
