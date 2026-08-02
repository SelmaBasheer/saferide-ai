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
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg)
        {
            throw pg.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => new DuplicateEntityException(
                    "A record with the same value already exists.",
                    ex
                ),
                PostgresErrorCodes.ForeignKeyViolation
                or PostgresErrorCodes.NotNullViolation
                or PostgresErrorCodes.CheckViolation => new DataIntegrityException(
                    "The data provided violates a database constraint.",
                    ex
                ),
                _ => new InfrastructureException("Database error on SaveChanges.", ex),
            };
        }
        catch (DbUpdateException ex)
        {
            throw new InfrastructureException("Database error on SaveChanges.", ex);
        }
    }
}
