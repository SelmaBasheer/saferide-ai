namespace SafeRide.Ai.Api.Contracts;

public sealed record AlertResponse(
    Guid Id,
    Guid TripId,
    string RouteCode,
    string RouteName,
    string Type,
    string Status,
    DateTime DetectedAtUtc,
    string? Classification,
    string? Reasoning,
    string? DraftMessage,
    double? Confidence,
    bool ClassifiedByModel,
    DateTime? ResolvedAtUtc
);
