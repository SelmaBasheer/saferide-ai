using Microsoft.EntityFrameworkCore;
using SafeRide.School.Application.Abstractions;

namespace SafeRide.School.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(DbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
