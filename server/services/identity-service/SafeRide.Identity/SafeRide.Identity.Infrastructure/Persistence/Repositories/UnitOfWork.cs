using Microsoft.EntityFrameworkCore;
using Npgsql;
using SafeRide.Identity.Domain.Repositories;
using SafeRide.Identity.Infrastructure.Exceptions;

namespace SafeRide.Identity.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(IdentityDbContext dbContext) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("Concurrency conflict on SaveChanges.", ex);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new DuplicateEntityException("Unique constraint violation on SaveChanges.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InfrastructureException("Database error on SaveChanges.", ex);
        }
    }
}
