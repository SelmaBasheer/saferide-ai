using SafeRide.Schools.Domain.Entities;

namespace SafeRide.Schools.Domain.Repositories;

public interface IPaymentRepository : IGenericRepository<Payment>
{
    /// The webhook's only handle on anything. Razorpay sends an order id; this
    /// turns it back into a school and a plan.
    Task<Payment?> GetByOrderIdAsync(string razorpayOrderId, CancellationToken ct = default);
}
