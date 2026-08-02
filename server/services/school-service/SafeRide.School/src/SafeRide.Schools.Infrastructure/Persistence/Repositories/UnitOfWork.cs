using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Infrastructure.Exceptions;

namespace SafeRide.Schools.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(DbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("Concurrency conflict on SaveChanges.", ex);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException { Number: 2627 or 2601 })
        {
            throw new DuplicateEntityException("Unique constraint violation on SaveChanges.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InfrastructureException("Database error on SaveChanges.", ex);
        }
    }
}
