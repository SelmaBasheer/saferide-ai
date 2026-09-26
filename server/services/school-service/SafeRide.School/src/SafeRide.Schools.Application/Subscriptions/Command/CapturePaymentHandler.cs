using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Application.Events;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Subscriptions.Command;

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

        /// <summary>
        /// The money arrived but bought nothing — the school already had a live
        /// subscription, or the plan has gone. The payment is still recorded,
        /// because a captured payment that exists nowhere in our database is
        /// money we took and cannot account for. Somebody has to refund it.
        /// </summary>
        CapturedWithoutSubscription,

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

        // Razorpay retries whenever we are slow or fail, so this same call
        // arrives more than once; only the first moves the row out of Created.
        if (!payment.TryCapture(razorpayPaymentId))
            return Outcome.Ignored;

        var plan = await plans.GetByIdAsync(payment.PlanId, ct);

        if (plan is null)
        {
            // Save first. Returning here without saving would leave the capture
            // only in memory and the row still reading Created — and Razorpay,
            // having had a 200, would never send it again.
            await unitOfWork.SaveChangesAsync(ct);
            return Outcome.CapturedWithoutSubscription;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // The school may have been activated offline while this checkout was
        // open. The money is real either way.
        var existing = await subscriptions.GetCurrentForSchoolAsync(payment.SchoolId, ct);

        if (existing is not null)
        {
            await unitOfWork.SaveChangesAsync(ct);
            return Outcome.CapturedWithoutSubscription;
        }

        var subscription = Subscription.Start(payment.SchoolId, plan, today);

        await subscriptions.AddAsync(subscription, ct);
        payment.LinkSubscription(subscription.Id);

        // One save, so the capture flag and the subscription are written in the
        // same transaction. Either both happen or neither does.
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
