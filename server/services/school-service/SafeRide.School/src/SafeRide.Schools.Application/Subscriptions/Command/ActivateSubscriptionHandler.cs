using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Application.Events;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Enums;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Subscriptions.Command;

/// <summary>
/// Records a subscription without any payment gateway involved.
///
/// This exists for two reasons. It lets the whole lifecycle — active, grace,
/// expired, and the bus limit that follows from it — be built and tested
/// before Razorpay is in the picture, so that when Razorpay misbehaves you
/// know the problem is Razorpay. And schools really do pay by cheque, so a
/// super admin recording an offline payment is a feature, not scaffolding.
/// </summary>
public sealed class ActivateSubscriptionHandler(
    ISubscriptionRepository subscriptions,
    ISubscriptionPlanRepository plans,
    IGenericRepository<School> schools,
    IUnitOfWork unitOfWork,
    IEventPublisher publisher
)
{
    public async Task<Result<Guid>> ActivateAsync(
        Guid schoolId,
        Guid planId,
        DateOnly? startsOn,
        CancellationToken ct
    )
    {
        var school = await schools.GetByIdAsync(schoolId, ct);

        if (school is null)
            return Result.Failure<Guid>(SchoolErrors.SchoolNotFound);

        if (school.Status != SchoolStatus.Approved)
            return Result.Failure<Guid>(SubscriptionErrors.SchoolNotApproved);

        var plan = await plans.GetByIdAsync(planId, ct);

        if (plan is null || !plan.IsActive)
            return Result.Failure<Guid>(SubscriptionErrors.PlanNotAvailable);

        // Active and InGrace both count. Selling a second subscription to a
        // school inside its grace period would leave two rows disagreeing
        // about when service ends.
        var existing = await subscriptions.GetCurrentForSchoolAsync(schoolId, ct);

        if (existing is not null)
            return Result.Failure<Guid>(SubscriptionErrors.AlreadySubscribed);

        var subscription = Subscription.Start(
            schoolId,
            plan,
            startsOn ?? DateOnly.FromDateTime(DateTime.UtcNow)
        );

        await subscriptions.AddAsync(subscription, ct);
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

        return Result.Success(subscription.Id);
    }
}
