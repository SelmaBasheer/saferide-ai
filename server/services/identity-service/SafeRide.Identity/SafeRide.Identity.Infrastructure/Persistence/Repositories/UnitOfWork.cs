using SafeRide.Identity.Domain.Repositories;

namespace SafeRide.Identity.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(IdentityDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct = default) => dbContext.SaveChangesAsync(ct);
}
