namespace SafeRide.School.Domain.Repositories;

// DB-agnostic repository contract. Implement per service with EF Core, Dapper, Mongo, etc.
public interface IGenericRepository<T>
    where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}
