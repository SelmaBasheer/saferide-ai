using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Abstractions;

public interface IPaymentFactRepository
{
    Task InsertAsync(PaymentFactRow row, CancellationToken ct);
}
