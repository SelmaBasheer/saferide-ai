using SafeRide.Schools.Domain.Common;

namespace SafeRide.Schools.Domain.Entities;

public class SubscriptionPlan : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    /// <summary>
    /// Whole paise, never rupees and never a decimal. Razorpay works in paise
    /// as integers, and money in a double drifts: 499.99 becomes 499.98999999
    /// after enough arithmetic.
    /// </summary>
    public long PriceInPaise { get; private set; }

    /// Null means unlimited. A sentinel like int.MaxValue would work until
    /// someone wrote a comparison and forgot about it.
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
        Validate(name, priceInPaise, durationMonths);

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
    /// Every field is editable, including price and bus limit, because a
    /// subscription copies both at purchase. Changing a plan changes what the
    /// next school pays — never what an existing one already agreed to.
    /// </summary>
    public void Update(
        string name,
        string? description,
        long priceInPaise,
        int? busLimit,
        int durationMonths
    )
    {
        Validate(name, priceInPaise, durationMonths);

        Name = name.Trim();
        Description = description?.Trim();
        PriceInPaise = priceInPaise;
        BusLimit = busLimit;
        DurationMonths = durationMonths;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Plans are deactivated, never deleted. Subscriptions and payments point
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

    // Programmer errors, not user errors. The handler checks the same rules and
    // returns proper error codes; this stops an impossible plan existing if
    // anyone calls the entity directly.
    private static void Validate(string name, long priceInPaise, int durationMonths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(priceInPaise);
        ArgumentOutOfRangeException.ThrowIfLessThan(durationMonths, 1);
    }
}
