namespace SafeRide.Schools.Api.Contracts;

/// <summary>
/// Price travels as paise, the same integer the database and Razorpay use.
/// The browser divides by 100 to show rupees. Sending 499.99 over the wire
/// would mean parsing a decimal in three places and hoping they agree.
/// </summary>
public sealed record CreatePlanRequest(
    string Name,
    string? Description,
    long PriceInPaise,
    int? BusLimit,
    int DurationMonths
);

public sealed record PlanDto(
    Guid Id,
    string Name,
    string? Description,
    long PriceInPaise,
    int? BusLimit,
    int DurationMonths,
    bool IsActive
);
