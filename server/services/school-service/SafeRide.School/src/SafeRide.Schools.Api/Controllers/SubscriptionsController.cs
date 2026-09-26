using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeRide.Schools.Api.Common;
using SafeRide.Schools.Api.Contracts;
using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Application.Subscriptions.Command;
using SafeRide.Schools.Application.Subscriptions.Query;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Enums;
using Serilog.Core;

namespace SafeRide.Schools.Api.Controllers;

[Route("api/subscriptions")]
[ApiController]
public class SubscriptionsController(
    ActivateSubscriptionHandler activate,
    GetSubscriptionsHandler query,
    StartCheckoutHandler startCheckout,
    CapturePaymentHandler capture,
    IPaymentGateway gateway,
    ILogger<SubscriptionsController> logger
) : ControllerBase
{
    /// Recording a payment that happened outside the system. A school admin
    /// must never be able to grant themselves a subscription.
    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("activate")]
    public async Task<IActionResult> Activate(
        ActivateSubscriptionRequest request,
        CancellationToken ct
    )
    {
        var result = await activate.ActivateAsync(
            request.SchoolId,
            request.PlanId,
            request.StartsOn,
            ct
        );

        return result.ToApiResponse(id => new { id });
    }

    [Authorize(Roles = "SchoolAdmin")]
    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await query.GetMineAsync(User.GetUserId(), ct);
        return result.ToApiResponse(s => ToDto(s, null));
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] SubscriptionStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default
    )
    {
        var (items, total) = await query.SearchAsync(status, page, pageSize, ct);

        var dtos = items.Select(i => ToDto(i.Subscription, i.SchoolName)).ToList();

        return Ok(
            ApiResponse<PagedResult<SubscriptionDto>>.Ok(
                new PagedResult<SubscriptionDto>(dtos, total, page, pageSize)
            )
        );
    }

    /// Creates an order at the gateway and hands the browser what it needs to
    /// open checkout. Nothing is activated here — only the webhook does that.
    [Authorize(Roles = "SchoolAdmin")]
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(CheckoutRequest request, CancellationToken ct)
    {
        var result = await startCheckout.StartAsync(User.GetUserId(), request.PlanId, ct);
        return result.ToApiResponse(s => s);
    }

    private static SubscriptionDto ToDto(Subscription s, string? schoolName) =>
        new(
            s.Id,
            s.SchoolId,
            schoolName,
            s.PlanName,
            s.PriceInPaise,
            s.BusLimit,
            s.StartsOn,
            s.EndsOn,
            s.GraceEndsOn,
            s.Status,
            s.DaysRemaining(DateOnly.FromDateTime(DateTime.UtcNow))
        );

    /// <summary>
    /// Razorpay calling us. No token, because Razorpay cannot log in — the
    /// signature is the entire authentication, which is why this reads the raw
    /// body rather than a bound model: the signature is over the exact bytes,
    /// and deserialising then re-serialising would change them.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers["X-Razorpay-Signature"].ToString();

        if (!gateway.VerifyWebhookSignature(body, signature))
        {
            logger.LogWarning("Rejected a webhook with an invalid signature");
            return BadRequest();
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var eventType = root.TryGetProperty("event", out var e) ? e.GetString() : null;

        if (eventType is not ("payment.captured" or "payment.failed"))
        {
            // Razorpay sends more event types than we subscribe to. Anything we
            // do not handle is acknowledged rather than retried forever.
            return Ok();
        }

        if (
            !root.TryGetProperty("payload", out var payload)
            || !payload.TryGetProperty("payment", out var payment)
            || !payment.TryGetProperty("entity", out var entity)
        )
        {
            logger.LogWarning("Webhook {Event} had an unexpected shape", eventType);
            return Ok();
        }

        var paymentId = entity.TryGetProperty("id", out var p)
            ? p.GetString() ?? string.Empty
            : string.Empty;
        var orderId = entity.TryGetProperty("order_id", out var o) ? o.GetString() : null;

        if (string.IsNullOrEmpty(orderId))
            return Ok();

        if (eventType == "payment.failed")
        {
            await capture.MarkFailedAsync(orderId, paymentId, ct);
            logger.LogInformation("Payment failed for order {OrderId}", orderId);
            return Ok();
        }

        var outcome = await capture.CaptureAsync(orderId, paymentId, ct);

        if (outcome == CapturePaymentHandler.Outcome.CapturedWithoutSubscription)
        {
            // Money taken, nothing sold. Loud, because somebody has to refund it.
            logger.LogError(
                "Payment captured for order {OrderId} but no subscription was created",
                orderId
            );
        }
        else
        {
            logger.LogInformation("Webhook for order {OrderId}: {Outcome}", orderId, outcome);
        }

        return Ok();
    }
}
