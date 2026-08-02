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
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql)
        {
            throw sql.Number switch
            {
                2627 or 2601 => // unique constraint / index
                new DuplicateEntityException("A record with the same value already exists.", ex),
                515 or 547 => // 515 = NULL into NOT NULL, 547 = FK/check constraint
                new DataIntegrityException("The data provided violates a database constraint.", ex),
                _ => new InfrastructureException("Database error on SaveChanges.", ex),
            };
        }
        catch (DbUpdateException ex)
        {
            throw new InfrastructureException("Database error on SaveChanges.", ex);
        }
    }
}
