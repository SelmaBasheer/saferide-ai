using DriverService.Domain;
using Microsoft.EntityFrameworkCore;

namespace DriverService.Persistence;

public class DriverDbContext(DbContextOptions<DriverDbContext> options) : DbContext(options)
{
    public DbSet<Driver> Drivers => Set<Driver>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Driver>(e =>
        {
            e.HasIndex(d => new { d.SchoolId, d.Email }).IsUnique(); // unique per tenant
            e.HasIndex(d => d.LicenseExpiryDate); // for the expiry scan
            e.Property(d => d.FirstName).HasMaxLength(75);
            e.Property(d => d.LastName).HasMaxLength(75);
            e.Property(d => d.Email).HasMaxLength(255);
            e.Property(d => d.Status);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.Property(m => m.Type).HasMaxLength(100);
            e.HasIndex(m => m.ProcessedAt).HasFilter("\"ProcessedAt\" IS NULL"); // relay scans pending only
        });
    }
}
