namespace SafeRide.Schools.Domain.Enums;

public enum PaymentStatus
{
    /// An order exists at Razorpay. Nobody has paid yet, and most of these
    /// will stay here forever — people open checkout and change their mind.
    Created = 0,
    Captured = 1,
    Failed = 2,
}
