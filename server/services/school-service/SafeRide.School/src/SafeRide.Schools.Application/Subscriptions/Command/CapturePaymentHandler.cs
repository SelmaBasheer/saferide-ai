using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Application.Events;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Subscriptions.Command;

/// <summary>
/// Turns a captured payment into a subscription.
///
/// Called only from the webhook. The browser also learns that the payment
/// succeeded, and that is deliberately ignored — anyone with devtools open can
/// fire the browser's success callback without paying. Only a request the
/// gateway signed counts.
/// </summary>
public sealed class CapturePaymentHandler(
    IPaymentRepository payments,
    ISubscriptionPlanRepository plans,
    ISubscriptionRepository subscriptions,
    IUnitOfWork unitOfWork,
    IEventPublisher publisher
)
{
    public enum Outcome
    {
        /// A subscription was created. Only the first delivery gets this.
        Activated,

        /// Nothing to do — already captured, or an order we do not know about.
        Ignored,
    }

    public async Task<Outcome> CaptureAsync(
        string razorpayOrderId,
        string razorpayPaymentId,
        CancellationToken ct
    )
    {
        var payment = await payments.GetByOrderIdAsync(razorpayOrderId, ct);

        // An order we never created. Someone else's webhook, or a test fired
        // from the dashboard. Nothing to retry.
        if (payment is null)
            return Outcome.Ignored;

        // The idempotency gate. Razorpay retries whenever we are slow or fail,
        // so this same call arrives more than once; only the first moves the row
        // out of Created.
        if (!payment.TryCapture(razorpayPaymentId))
            return Outcome.Ignored;

        var plan = await plans.GetByIdAsync(payment.PlanId, ct);

        if (plan is null)
            return Outcome.Ignored;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Two deliveries arriving at the same instant would both pass the check
        // above in memory; this is the second line of defence, and the
        // transaction below is the third.
        var existing = await subscriptions.GetCurrentForSchoolAsync(payment.SchoolId, ct);

        if (existing is not null && existing.IsServiceable(today))
            return Outcome.Ignored;

        var subscription = Subscription.Start(payment.SchoolId, plan, today);

        await subscriptions.AddAsync(subscription, ct);
        payment.LinkSubscription(subscription.Id);

        // One save, so the capture flag and the subscription are written in the
        // same transaction. Either both happen or neither does — there is no
        // state where the payment is marked used but bought nothing.
        await unitOfWork.SaveChangesAsync(ct);

        await publisher.PublishAsync(
            MessagingConstants.SchoolEventsExchange,
            MessagingConstants.SchoolSubscriptionChangedKey,
            new SchoolSubscriptionChanged(
                subscription.SchoolId,
                subscription.Status.ToString(),
                subscription.BusLimit,
                subscription.EndsOn,
                DateTime.UtcNow
            ),
            ct
        );

        return Outcome.Activated;
    }

    public async Task MarkFailedAsync(
        string razorpayOrderId,
        string razorpayPaymentId,
        CancellationToken ct
    )
    {
        var payment = await payments.GetByOrderIdAsync(razorpayOrderId, ct);

        if (payment is null)
            return;

        // A failed card is not an expired subscription. This records the
        // attempt and changes nothing else.
        payment.Fail(razorpayPaymentId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
