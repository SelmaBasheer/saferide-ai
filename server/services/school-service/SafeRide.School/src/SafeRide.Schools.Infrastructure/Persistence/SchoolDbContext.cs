using Microsoft.EntityFrameworkCore;
using SafeRide.Schools.Domain.Entities;

namespace SafeRide.Schools.Infrastructure.Persistence;

public sealed class SchoolDbContext(DbContextOptions<SchoolDbContext> options) : DbContext(options)
{
    public DbSet<School> Schools => Set<School>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<School>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).HasMaxLength(200).IsRequired();
            e.Property(s => s.Address).HasMaxLength(500).IsRequired();
            e.Property(s => s.City).HasMaxLength(100).IsRequired();
            e.Property(s => s.District).HasMaxLength(100).IsRequired();
            e.Property(s => s.State).HasMaxLength(100).IsRequired();
            e.Property(s => s.Pincode).HasMaxLength(10).IsRequired();
            e.Property(s => s.AdminEmail).HasMaxLength(256).IsRequired();
            e.Property(s => s.AdminFirstName).HasMaxLength(200).IsRequired();
            e.Property(s => s.AdminLastName).HasMaxLength(200).IsRequired();
            e.Property(s => s.AdminPhone).HasMaxLength(20).IsRequired();
            e.HasIndex(s => s.AdminUserId); //look schools up by their admin
        });
    }
}
