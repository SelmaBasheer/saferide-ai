namespace SafeRide.Schools.Application.Plans.Command;

public sealed record CreatePlanCommand(
    string Name,
    string? Description,
    long PriceInPaise,
    int? BusLimit,
    int DurationMonths
);
