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
    AnomalyToolbox toolbox,
    TemplateAnomalyClassifier fallback,
    ILogger<GeminiAnomalyClassifier> logger
) : IAnomalyClassifier
{
    private const string SystemPrompt = """
        You classify anomalies detected on school bus trips in Kerala, India.

        You receive a JSON context describing what the monitoring system observed.
        Decide the single most likely explanation and write a short note for the
        school office.

        You may look up history first using the tools provided. Use them when the
        numbers alone are ambiguous. Something extreme usually speaks for itself
        and needs no lookup. Do not call the same tool twice.

        For a RouteDeviation, choose from:
        - Traffic — low speed with a small deviation
        - Breakdown — near-zero speed with a deviation
        - UnauthorisedDetour — a real detour with no innocent explanation
        - StalePath — repeated deviations on the same route mean the stored path is
          probably out of date, not that the driver did anything wrong
        - DataError — more than 5 km off route is almost always a GPS glitch or a
          test position

        For a SkippedStop, choose from:
        - NoStudentsToCollect — nobody was waiting, so there was nothing to stop for
        - RunningBehindSchedule — the bus skipped ahead to make up time
        - TripEndedEarly — the trip ended before the route was finished; several
          missed stops in a row at the end of the route point at this
        - DataError — the stop's stored location may be wrong, so the arrival was
          never detected even though the bus was there

        Rules:
        - Never state a cause as certain, never promise a time, never name a child.
        - The message is for a school office clerk, not an engineer. Two sentences,
          plain language, no jargon.
        """;

    private const string VerdictPrompt =
        "Give your final answer now, as JSON matching the required schema.";

    /// Two attempts, not three. Each one can burn the full HTTP timeout, so the
    /// consumer is already waiting; a third attempt buys little and costs a lot.
    private const int MaxAttempts = 2;

    /// A loop with a model inside it needs a stop condition that is not the
    /// model's own judgement.
    private const int MaxToolRounds = 3;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// Gemini enforces this shape on the response, so the reply is valid JSON by
    /// construction rather than by asking the model nicely. The labels for both
    /// anomaly kinds live in one list because the schema is built once; the system
    /// prompt is what tells the model which subset applies.
    private static readonly object ResponseSchema = new
    {
        type = "OBJECT",
        properties = new
        {
            label = new
            {
                type = "STRING",
                @enum = new[]
                {
                    "Traffic",
                    "Breakdown",
                    "UnauthorisedDetour",
                    "StalePath",
                    "DataError",
                    "NoStudentsToCollect",
                    "RunningBehindSchedule",
                    "TripEndedEarly",
                },
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
                // The conversation is rebuilt per attempt. A retry after a failed
                // turn must not inherit half a dialogue.
                var conversation = new List<object>
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new
                            {
                                text = $"Anomaly type: {context.Type}\nRoute: {context.RouteCode} — {context.RouteName}\n\nContext:\n{context.ContextJson}",
                            },
                        },
                    },
                };

                // Two phases, because Gemini will not do both at once: a request
                // may offer tools, or enforce a response schema, but enforcing a
                // schema leaves the model no way to ask a question.
                await GatherAsync(conversation, context, ct);

                var classification = await DecideAsync(conversation, ct);

                // Null means the model answered unusably — a malformed reply, an
                // empty one. Asking again produces the same thing.
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

    /// Phase one. The model may ask for history; we answer and let it ask again,
    /// up to a limit. It ends when the model stops asking — which for an obvious
    /// case is immediately, on the first turn.
    private async Task GatherAsync(
        List<object> conversation,
        AnomalyContext context,
        CancellationToken ct
    )
    {
        for (var round = 1; round <= MaxToolRounds; round++)
        {
            var body = await SendAsync(
                new
                {
                    system_instruction = new { parts = new[] { new { text = SystemPrompt } } },
                    contents = conversation,
                    tools = new[] { AnomalyToolbox.Declarations },
                    generationConfig = new { thinkingConfig = new { thinkingLevel = "low" } },
                },
                ct
            );

            var parts = body?.Candidates?.FirstOrDefault()?.Content?.Parts ?? [];

            var calls = parts
                .Where(p => p.FunctionCall is not null)
                .Select(p => p.FunctionCall!)
                .ToList();

            if (calls.Count == 0)
            {
                logger.LogInformation(
                    "Model asked for no further lookups after {Rounds} round(s)",
                    round - 1
                );

                return;
            }

            // The model's own turn goes back verbatim, including the calls it
            // made. Without it the next request would show answers to questions
            // that were never asked.
            conversation.Add(new { role = "model", parts = parts.Select(ToRequestPart).ToArray() });

            var answers = new List<object>();

            foreach (var call in calls)
            {
                logger.LogInformation(
                    "Model called {Tool} in round {Round} of {Max}",
                    call.Name,
                    round,
                    MaxToolRounds
                );

                var result = await toolbox.InvokeAsync(call.Name, call.Args, context, ct);

                answers.Add(new { functionResponse = new { name = call.Name, response = result } });
            }

            conversation.Add(new { role = "user", parts = answers.ToArray() });
        }

        logger.LogWarning(
            "Model reached the {Max}-round lookup limit, asking for a verdict now",
            MaxToolRounds
        );
    }

    /// Phase two. Same conversation, no tools, schema enforced — so whatever the
    /// model learned in phase one is still in front of it, but the only thing it
    /// can do now is answer.
    private async Task<Classification?> DecideAsync(List<object> conversation, CancellationToken ct)
    {
        var contents = new List<object>(conversation)
        {
            new { role = "user", parts = new object[] { new { text = VerdictPrompt } } },
        };

        var body = await SendAsync(
            new
            {
                system_instruction = new { parts = new[] { new { text = SystemPrompt } } },
                contents,
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseSchema = ResponseSchema,
                    maxOutputTokens = _settings.MaxTokens,

                    // Gemini 3 counts its internal thinking against maxOutputTokens,
                    // which is how a 500-token budget produced truncated JSON earlier.
                    thinkingConfig = new { thinkingLevel = "low" },
                },
            },
            ct
        );

        var candidate = body?.Candidates?.FirstOrDefault();
        var text = candidate?.Content?.Parts?.FirstOrDefault(p => p.Text is not null)?.Text;

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

    /// One POST, with the status-code decision made in a single place so neither
    /// phase has to interpret HTTP for itself.
    private async Task<GeminiResponse?> SendAsync(object request, CancellationToken ct)
    {
        // Absolute URL on purpose: combining a relative path with BaseAddress can
        // percent-encode the colon in ":generateContent", which Google 404s on.
        var url =
            $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.Model}:generateContent";

        var response = await http.PostAsJsonAsync(url, request, ct);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GeminiResponse>(Json, ct);
        }

        var error = await response.Content.ReadAsStringAsync(ct);

        logger.LogError("Gemini returned {Status}: {Body}", (int)response.StatusCode, error);

        // 429 means "you are going too fast" and 5xx means "we are struggling".
        // Google says so itself: spikes in demand are usually temporary. Both are
        // worth another attempt. A 400 or a 403 is our mistake and never will be.
        if (IsTransientStatus(response.StatusCode))
        {
            throw new TransientModelException($"Gemini returned {(int)response.StatusCode}.");
        }

        throw new PermanentModelException($"Gemini returned {(int)response.StatusCode}.");
    }

    /// Turns a part we received back into a part we can send. A dictionary rather
    /// than an anonymous type because the thought signature is only sometimes
    /// present, and sending it as null is not the same as leaving it out.
    private static object ToRequestPart(GeminiPart part)
    {
        var result = new Dictionary<string, object>();

        if (part.FunctionCall is { } call)
        {
            result["functionCall"] = new { name = call.Name, args = call.Args };
        }
        else
        {
            result["text"] = part.Text ?? string.Empty;
        }

        if (!string.IsNullOrEmpty(part.ThoughtSignature))
        {
            result["thoughtSignature"] = part.ThoughtSignature;
        }

        return result;
    }

    private static bool IsTransientStatus(HttpStatusCode status) =>
        status == HttpStatusCode.TooManyRequests || (int)status >= 500;

    private static bool IsTransient(Exception ex) =>
        ex is TransientModelException or HttpRequestException or TaskCanceledException;

    /// Worth another attempt.
    private sealed class TransientModelException(string message) : Exception(message);

    /// Not worth another attempt. Kept distinct so IsTransient stays a statement
    /// about the failure rather than a list of everything else.
    private sealed class PermanentModelException(string message) : Exception(message);

    private sealed record GeminiResponse(List<GeminiCandidate>? Candidates);

    private sealed record GeminiCandidate(GeminiContent? Content, string? FinishReason);

    private sealed record GeminiContent(List<GeminiPart>? Parts);

    private sealed record GeminiPart(
        string? Text,
        GeminiFunctionCall? FunctionCall,
        string? ThoughtSignature
    );

    private sealed record GeminiFunctionCall(string Name, JsonElement Args);

    private sealed record ModelVerdict(
        string? Label,
        double? Confidence,
        string? Reasoning,
        string? Message
    );
}
