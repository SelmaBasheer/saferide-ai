using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Enums;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Application.Subscriptions.Command;

public sealed record CheckoutSession(
    string OrderId,
    long AmountInPaise,
    string Currency,
    string PublicKey,
    string PlanName,
    string SchoolName,
    string AdminEmail,
    string AdminPhone
);

public sealed class StartCheckoutHandler(
    ISchoolRepository schools,
    ISubscriptionPlanRepository plans,
    ISubscriptionRepository subscriptions,
    IPaymentRepository payments,
    IPaymentGateway gateway,
    IUnitOfWork unitOfWork
)
{
    public async Task<Result<CheckoutSession>> StartAsync(
        Guid adminUserId,
        Guid planId,
        CancellationToken ct
    )
    {
        var school = await schools.GetByAdminUserIdAsync(adminUserId, ct);

        if (school is null)
            return Result.Failure<CheckoutSession>(SchoolErrors.SchoolNotFound);

        if (school.Status != SchoolStatus.Approved)
            return Result.Failure<CheckoutSession>(SubscriptionErrors.SchoolNotApproved);

        var plan = await plans.GetByIdAsync(planId, ct);

        if (plan is null || !plan.IsActive)
            return Result.Failure<CheckoutSession>(SubscriptionErrors.PlanNotAvailable);

        var existing = await subscriptions.GetCurrentForSchoolAsync(school.Id, ct);

        if (existing is not null && existing.IsServiceable(DateOnly.FromDateTime(DateTime.UtcNow)))
            return Result.Failure<CheckoutSession>(SubscriptionErrors.AlreadySubscribed);

        // The amount comes from the plan we just loaded, never from the request.
        // A client that could name its own price would name a low one.
        var payment = Payment.Create(school.Id, plan.Id, string.Empty, plan.PriceInPaise);

        GatewayOrder order;

        try
        {
            // Razorpay caps the receipt at 40 characters, so the id goes in
            // without dashes.
            order = await gateway.CreateOrderAsync(plan.PriceInPaise, $"sub_{payment.Id:N}", ct);
        }
        catch (Exception)
        {
            return Result.Failure<CheckoutSession>(SubscriptionErrors.GatewayFailed);
        }

        // Saved only after the gateway agreed, so there are no orphan rows
        // pointing at orders that never existed.
        payment.AttachOrder(order.OrderId);

        await payments.AddAsync(payment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(
            new CheckoutSession(
                order.OrderId,
                order.AmountInPaise,
                order.Currency,
                gateway.PublicKey,
                plan.Name,
                school.Name,
                school.AdminEmail,
                school.AdminPhone
            )
        );
    }
}
