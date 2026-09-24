using SafeRide.Schools.Domain.Common;

namespace SafeRide.Schools.Domain.Entities;

public class SubscriptionPlan : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public long PriceInPaise { get; private set; }
    public int? BusLimit { get; private set; }
    public int DurationMonths { get; private set; }
    public bool IsActive { get; private set; }

    private SubscriptionPlan() { }

    public static SubscriptionPlan Create(
        string name,
        string? description,
        long priceInPaise,
        int? busLimit,
        int durationMonths
    )
    {
        // These are programmer errors, not user errors. The handler checks the
        // same things and returns a proper error code; this is the guard that
        // stops an impossible plan existing if anyone ever calls it directly.
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(priceInPaise);
        ArgumentOutOfRangeException.ThrowIfLessThan(durationMonths, 1);

        return new SubscriptionPlan
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            PriceInPaise = priceInPaise,
            BusLimit = busLimit,
            DurationMonths = durationMonths,
            IsActive = true,
        };
    }

    /// <summary>
    /// Plans are deactivated, never deleted. Payments and subscriptions point
    /// at a plan, and deleting one would orphan every record of what a school
    /// actually bought.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
