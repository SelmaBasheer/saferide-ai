namespace SafeRide.Schools.Application.Abstractions;

public sealed record GatewayOrder(string OrderId, long AmountInPaise, string Currency);

public interface IPaymentGateway
{
    /// The gateway's public identifier, safe to hand to a browser. It is not a
    /// secret — it only says which merchant a checkout belongs to.
    string PublicKey { get; }

    Task<GatewayOrder> CreateOrderAsync(
        long amountInPaise,
        string receipt,
        CancellationToken ct = default
    );

    /// True when this body really came from the gateway. The webhook carries no
    /// token and cannot log in, so the signature is the entire authentication.
    bool VerifyWebhookSignature(string rawBody, string signature);
}
