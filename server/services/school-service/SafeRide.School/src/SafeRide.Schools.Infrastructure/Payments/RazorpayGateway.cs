using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SafeRide.Schools.Application.Abstractions;

namespace SafeRide.Schools.Infrastructure.Payments;

public sealed class RazorpayGateway(HttpClient http, IOptions<RazorpaySettings> options)
    : IPaymentGateway
{
    private readonly RazorpaySettings _settings = options.Value;

    public string PublicKey => _settings.KeyId;

    public async Task<GatewayOrder> CreateOrderAsync(
        long amountInPaise,
        string receipt,
        CancellationToken ct = default
    )
    {
        var response = await http.PostAsJsonAsync(
            "v1/orders",
            new
            {
                amount = amountInPaise,
                currency = "INR",
                receipt,
            },
            ct
        );

        response.EnsureSuccessStatusCode();

        var order =
            await response.Content.ReadFromJsonAsync<OrderResponse>(ct)
            ?? throw new InvalidOperationException("Razorpay returned an empty order.");

        return new GatewayOrder(order.Id, order.Amount, order.Currency);
    }

    public bool VerifyWebhookSignature(string rawBody, string signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));

        var computed = Convert
            .ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody)))
            .ToLowerInvariant();

        // Fixed-time comparison, not ==. A normal string comparison returns as
        // soon as two characters differ, and the time it took leaks how much of
        // the signature was guessed correctly. That is enough to forge one, given
        // enough attempts.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signature)
        );
    }

    private sealed record OrderResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("amount")] long Amount,
        [property: JsonPropertyName("currency")] string Currency
    );
}
