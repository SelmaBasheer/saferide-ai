namespace SafeRide.Schools.Infrastructure.Payments;

public sealed class RazorpaySettings
{
    public const string SectionName = "Razorpay";

    public string KeyId { get; set; } = string.Empty;

    /// Authenticates our calls to Razorpay.
    public string KeySecret { get; set; } = string.Empty;

    /// A different secret, used only to verify what Razorpay sends us. Razorpay
    /// generates it when you create the webhook, and mixing the two up is the
    /// most common reason a signature check fails.
    public string WebhookSecret { get; set; } = string.Empty;
}
