using Microsoft.EntityFrameworkCore;
using SafeRide.School.Domain.Repositories;

namespace SafeRide.School.Infrastructure.Persistence.Repositories;

// EF Core implementation of the generic repository (relational providers:
// Postgres, SQL Server, MySQL). Specific repositories inherit this and add
// their own intention-revealing queries. Mongo services provide their own.
public class GenericRepository<T>(DbContext context) : IGenericRepository<T>
    where T : class
{
    protected DbContext Context { get; } = context;
    protected DbSet<T> Set => Context.Set<T>();

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await Set.FindAsync(new object?[] { id }, ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default) =>
        await Set.AddAsync(entity, ct);

    public virtual void Update(T entity) => Set.Update(entity);

    public virtual void Remove(T entity) => Set.Remove(entity);
}
