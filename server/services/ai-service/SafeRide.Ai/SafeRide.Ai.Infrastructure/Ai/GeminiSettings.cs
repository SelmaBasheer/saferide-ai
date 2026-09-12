namespace SafeRide.Ai.Infrastructure.Ai;

public sealed class GeminiSettings
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    // Flash models are on the free tier and ample for a short classification.
    public string Model { get; set; } = "gemini-2.5-flash";

    public int MaxTokens { get; set; } = 500;
}
