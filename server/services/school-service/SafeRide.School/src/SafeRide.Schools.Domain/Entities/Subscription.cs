using SafeRide.Schools.Domain.Common;
using SafeRide.Schools.Domain.Enums;

namespace SafeRide.Schools.Domain.Entities;

public class Subscription : BaseEntity
{
    /// <summary>
    /// A school bus service that stops without warning is a safety problem,
    /// not just a billing one. Seven days of everything still working, with a
    /// red banner, before anything is taken away.
    /// </summary>
    public const int GraceDays = 7;

    /// How many days before the end date a warning goes out.
    public static readonly int[] WarningDays = [7, 3, 1];

    public Guid SchoolId { get; private set; }
    public Guid PlanId { get; private set; }

    // ----- copied from the plan at purchase, never read through -----
    public string PlanName { get; private set; } = null!;
    public long PriceInPaise { get; private set; }
    public int? BusLimit { get; private set; }

    public DateOnly StartsOn { get; private set; }
    public DateOnly EndsOn { get; private set; }

    /// <summary>
    /// Only ever Active or Cancelled. Expiry is not stored, because storing it
    /// would mean something had to run to write it down — and then the row is
    /// wrong from the moment that job fails until someone notices.
    /// </summary>
    public SubscriptionStatus Status { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    /// <summary>
    /// The one piece of state the worker does keep. The check runs hourly, so
    /// without this a school seven days from expiry would get twenty-four
    /// emails in a day. One date turns "has this been sent" into a question
    /// with an answer.
    /// </summary>
    public DateOnly? LastWarningSentOn { get; private set; }

    public DateOnly GraceEndsOn => EndsOn.AddDays(GraceDays);

    private Subscription() { }

    public static Subscription Start(Guid schoolId, SubscriptionPlan plan, DateOnly startsOn) =>
        new()
        {
            SchoolId = schoolId,
            PlanId = plan.Id,
            PlanName = plan.Name,
            PriceInPaise = plan.PriceInPaise,
            BusLimit = plan.BusLimit,
            StartsOn = startsOn,

            // The last day of service, not the first day without it.
            EndsOn = startsOn.AddMonths(plan.DurationMonths).AddDays(-1),
            Status = SubscriptionStatus.Active,
        };

    /// <summary>
    /// What this subscription actually is today. Cancelling is a decision
    /// somebody made, so it is stored. Expiring is the passage of time, so it
    /// is calculated.
    /// </summary>
    public SubscriptionStatus EffectiveStatus(DateOnly today)
    {
        if (Status == SubscriptionStatus.Cancelled)
            return SubscriptionStatus.Cancelled;

        if (today <= EndsOn)
            return SubscriptionStatus.Active;

        if (today <= GraceEndsOn)
            return SubscriptionStatus.InGrace;

        return SubscriptionStatus.Expired;
    }

    public bool IsServiceable(DateOnly today) =>
        EffectiveStatus(today) is SubscriptionStatus.Active or SubscriptionStatus.InGrace;

    public int DaysRemaining(DateOnly today) => EndsOn.DayNumber - today.DayNumber;

    /// True when today is a warning day and nothing has gone out yet today.
    public bool ShouldWarn(DateOnly today) =>
        Status == SubscriptionStatus.Active
        && LastWarningSentOn != today
        && WarningDays.Contains(DaysRemaining(today));

    public void MarkWarningSent(DateOnly today)
    {
        LastWarningSentOn = today;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == SubscriptionStatus.Cancelled)
            return;

        Status = SubscriptionStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
