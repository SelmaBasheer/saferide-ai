using Microsoft.EntityFrameworkCore;
using SafeRide.Schools.Application.Abstractions;

namespace SafeRide.Schools.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(DbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
