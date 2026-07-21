using Microsoft.EntityFrameworkCore;
using SafeRide.Identity.Domain.Entities;
using SafeRide.Identity.Domain.ValueObjects;

namespace SafeRide.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity
                .Property(u => u.Email)
                .HasConversion(e => e.Value, v => Email.Create(v))
                .HasMaxLength(256)
                .IsRequired();
            entity.Property(u => u.FirstName).HasMaxLength(200).IsRequired();
            entity.Property(u => u.LastName).HasMaxLength(200).IsRequired();
            entity
                .Property(u => u.Phone)
                .HasConversion(p => p.Value, v => Phone.Create(v))
                .HasMaxLength(20)
                .IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(rt => rt.TokenHash).IsUnique();
        });
    }
}
