using SafeRide.Schools.Domain.Common;

namespace SafeRide.Schools.Application.Common;

public static class PlanErrors
{
    public static readonly Error NotFound = new("Plan.NotFound", "Plan not found.");

    public static readonly Error NameRequired = new(
        "Plan.NameRequired",
        "A plan name is required."
    );

    public static readonly Error NameTaken = new(
        "Plan.NameTaken",
        "A plan with that name already exists."
    );

    public static readonly Error PriceInvalid = new(
        "Plan.PriceInvalid",
        "Price must be zero or more, in paise."
    );

    public static readonly Error DurationInvalid = new(
        "Plan.DurationInvalid",
        "Duration must be between 1 and 36 months."
    );

    public static readonly Error BusLimitInvalid = new(
        "Plan.BusLimitInvalid",
        "Bus limit must be at least 1, or left empty for unlimited."
    );
}
