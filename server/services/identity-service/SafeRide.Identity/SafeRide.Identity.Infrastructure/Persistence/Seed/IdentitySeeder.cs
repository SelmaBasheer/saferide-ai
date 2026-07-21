using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SafeRide.Identity.Application.Abstractions;
using SafeRide.Identity.Domain.Entities;
using SafeRide.Identity.Domain.ValueObjects;

namespace SafeRide.Identity.Infrastructure.Persistence.Seed;

public class IdentitySeeder(
    IdentityDbContext context,
    IPasswordHasher passwordHasher,
    IConfiguration configuration
)
{
    public async Task SeedAsync() => await SeedSuperAdminAsync();

    private async Task SeedSuperAdminAsync()
    {
        var exists = await context.Users.AnyAsync(u => u.Role == Domain.Enums.UserRole.SuperAdmin);
        if (exists)
            return;

        var email = Email.Create(configuration["Seed:SuperAdminEmail"] ?? "admin@saferide.ai");
        var phone = Phone.Create(configuration["Seed:SuperAdminPhone"] ?? "+918848958139");
        var password =
            configuration["Seed:SuperAdminPassword"]
            ?? throw new InvalidOperationException("Seed:SuperAdminPassword must be configured.");

        var superAdmin = User.CreateSuperAdmin(
            email,
            passwordHasher.HashPassword(password),
            "Saferide",
            "Admin",
            phone
        );
        context.Users.Add(superAdmin);
        await context.SaveChangesAsync();
    }
}
