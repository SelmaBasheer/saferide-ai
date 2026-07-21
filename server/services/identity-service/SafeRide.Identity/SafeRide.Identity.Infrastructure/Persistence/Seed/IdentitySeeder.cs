using Microsoft.EntityFrameworkCore;
using SafeRide.Identity.Application.Abstractions;
using SafeRide.Identity.Domain.Entities;
using SafeRide.Identity.Domain.ValueObjects;

namespace SafeRide.Identity.Infrastructure.Persistence.Seed;

public class IdentitySeeder(IdentityDbContext context, IPasswordHasher passwordHasher)
{
    public async Task SeedAsync()
    {
        await SeedSuperAdminAsync();
    }

    private async Task SeedSuperAdminAsync()
    {
        var exists = await context.Users.AnyAsync(u => u.Role == Domain.Enums.UserRole.SuperAdmin);
        if (exists)
            return; // skip — already seeded

        Email email = Email.Create("admin@saferide.ai");
        Phone phone = Phone.Create("+91 8848958139");
        var passwordHash = passwordHasher.HashPassword("Admin@123");

        var superAdmin = User.CreateSuperAdmin(email, passwordHash, "Saferide", "Admin", phone);

        await context.Users.AddAsync(superAdmin);
        await context.SaveChangesAsync();

        Console.WriteLine("SuperAdmin user seeded successfully!");
    }
}
