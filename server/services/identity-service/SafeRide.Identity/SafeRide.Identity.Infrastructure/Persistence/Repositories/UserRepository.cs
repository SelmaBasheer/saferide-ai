using Microsoft.EntityFrameworkCore;
using SafeRide.Identity.Application.Abstractions;
using SafeRide.Identity.Domain.Entities;
using SafeRide.Identity.Domain.ValueObjects;

namespace SafeRide.Identity.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(IdentityDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var target = Email.Create(email);
        return dbContext.Users.FirstOrDefaultAsync(u => u.Email == target, ct);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        var target = Email.Create(email);
        return dbContext.Users.AnyAsync(u => u.Email == target, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await dbContext.Users.AddAsync(user, ct);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
}
