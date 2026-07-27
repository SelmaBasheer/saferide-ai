using Microsoft.EntityFrameworkCore;
using SafeRide.Identity.Domain.Entities;
using SafeRide.Identity.Domain.Repositories;
using SafeRide.Identity.Domain.ValueObjects;

namespace SafeRide.Identity.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(IdentityDbContext dbContext)
    : GenericRepository<User>(dbContext),
        IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var target = Email.Create(email);
        return await Set.FirstOrDefaultAsync(u => u.Email == target, ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        var target = Email.Create(email);
        return await Set.AnyAsync(u => u.Email == target, ct);
    }
}
