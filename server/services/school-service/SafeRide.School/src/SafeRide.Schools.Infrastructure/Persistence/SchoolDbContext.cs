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
            e.Property(d => d.Id).ValueGeneratedNever();
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

            // Extended profile
            e.Property(s => s.LegalName).HasMaxLength(300);
            e.Property(s => s.RegistrationNumber).HasMaxLength(100);
            e.Property(s => s.AuthorizedPersonName).HasMaxLength(200);
            e.Property(s => s.AuthorizedPersonDesignation).HasMaxLength(100);
            e.Property(s => s.OfficialPhone).HasMaxLength(20);
            e.Property(s => s.OfficialEmail).HasMaxLength(256);
            e.Property(s => s.RejectionReason).HasMaxLength(1000);

            // One school -> many documents; deleting a school deletes its doc rows
            e.HasMany(s => s.Documents)
                .WithOne()
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(s => s.Documents).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<SchoolDocument>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).ValueGeneratedNever();
            e.Property(d => d.FileName).HasMaxLength(260).IsRequired();
            e.Property(d => d.BlobKey).HasMaxLength(500).IsRequired();
            e.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
            e.HasIndex(d => new { d.SchoolId, d.Type }).IsUnique();
            e.ToTable("SchoolDocuments");
        });
    }
}
