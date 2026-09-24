using SafeRide.Schools.Domain.Common;

namespace SafeRide.Schools.Application.Common;

public static class SubscriptionErrors
{
    public static readonly Error NotFound = new("Subscription.NotFound", "No subscription found.");

    public static readonly Error SchoolNotApproved = new(
        "Subscription.SchoolNotApproved",
        "Only an approved school can be subscribed."
    );

    public static readonly Error AlreadySubscribed = new(
        "Subscription.AlreadySubscribed",
        "This school already has a live subscription."
    );

    public static readonly Error PlanNotAvailable = new(
        "Subscription.PlanNotAvailable",
        "That plan does not exist or is no longer offered."
    );
}
