using Microsoft.EntityFrameworkCore;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Infrastructure.Persistence.Repositories;

public class PaymentRepository(SchoolDbContext context)
    : GenericRepository<Payment>(context),
        IPaymentRepository
{
    public Task<Payment?> GetByOrderIdAsync(
        string razorpayOrderId,
        CancellationToken ct = default
    ) => Set.FirstOrDefaultAsync(p => p.RazorpayOrderId == razorpayOrderId, ct);
}
