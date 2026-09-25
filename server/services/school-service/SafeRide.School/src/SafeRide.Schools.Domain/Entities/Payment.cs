using SafeRide.Schools.Domain.Common;
using SafeRide.Schools.Domain.Enums;

namespace SafeRide.Schools.Domain.Entities;

/// <summary>
/// One attempt to pay for one plan.
///
/// Created when the browser asks for a checkout, updated when Razorpay says the
/// money arrived. It is the link between an order id — the only thing a webhook
/// carries — and the school and plan it was for.
/// </summary>
public class Payment : BaseEntity
{
    public Guid SchoolId { get; private set; }
    public Guid PlanId { get; private set; }

    /// Razorpay's id for the order we asked them to create. Unique, and the
    /// key the webhook is looked up by.
    public string RazorpayOrderId { get; private set; } = null!;

    /// Their id for the actual payment. Null until it succeeds.
    public string? RazorpayPaymentId { get; private set; }

    /// <summary>
    /// Copied from the plan when the order is created, never taken from the
    /// browser. A client that could name its own price would name a low one.
    /// </summary>
    public long AmountInPaise { get; private set; }

    public PaymentStatus Status { get; private set; }
    public DateTime? CapturedAtUtc { get; private set; }

    /// What this payment bought. Null until the webhook creates it.
    public Guid? SubscriptionId { get; private set; }

    private Payment() { }

    public static Payment Create(
        Guid schoolId,
        Guid planId,
        string razorpayOrderId,
        long amountInPaise
    ) =>
        new()
        {
            SchoolId = schoolId,
            PlanId = planId,
            RazorpayOrderId = razorpayOrderId,
            AmountInPaise = amountInPaise,
            Status = PaymentStatus.Created,
        };

    /// <summary>
    /// The idempotency guard, and the reason it returns a bool rather than
    /// throwing.
    ///
    /// Razorpay retries a webhook whenever the response is slow or fails, so
    /// the same payment.captured arrives more than once. The first call moves
    /// the row to Captured and returns true — that caller creates the
    /// subscription. Every later call returns false and creates nothing.
    ///
    /// The database is what makes this safe, not the code: the transition and
    /// the subscription are written in one transaction, so two deliveries
    /// racing each other cannot both win.
    /// </summary>
    public bool TryCapture(string razorpayPaymentId)
    {
        if (Status == PaymentStatus.Captured)
            return false;

        RazorpayPaymentId = razorpayPaymentId;
        Status = PaymentStatus.Captured;
        CapturedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;

        return true;
    }

    public void LinkSubscription(Guid subscriptionId)
    {
        SubscriptionId = subscriptionId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Fail(string? razorpayPaymentId)
    {
        if (Status == PaymentStatus.Captured)
            return;

        RazorpayPaymentId = razorpayPaymentId;
        Status = PaymentStatus.Failed;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// Set once, immediately after the gateway creates the order. Separate from
    /// Create because the id does not exist until the gateway has answered, and
    /// calling the gateway before knowing the amount would be the wrong order.
    public void AttachOrder(string razorpayOrderId)
    {
        RazorpayOrderId = razorpayOrderId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
