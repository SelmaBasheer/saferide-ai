using Microsoft.EntityFrameworkCore;
using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Domain.Common;
using SafeRide.Schools.Domain.Entities;

namespace SafeRide.Schools.Infrastructure.Persistence;

public sealed class SchoolDbContext(
    DbContextOptions<SchoolDbContext> options,
    ITenantProvider tenantProvider
) : DbContext(options)
{
    public DbSet<School> Schools => Set<School>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Payment> Payments => Set<Payment>();

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
            e.HasIndex(d => d.TenantId);
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).ValueGeneratedNever();
            e.Property(d => d.FileName).HasMaxLength(260).IsRequired();
            e.Property(d => d.BlobKey).HasMaxLength(500).IsRequired();
            e.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
            e.HasIndex(d => new { d.SchoolId, d.Type }).IsUnique();
            e.ToTable("SchoolDocuments");
        });

        // Plans belong to the platform, not to a school, so deliberately no
        // TenantId and no query filter. A school admin choosing a plan must be
        // able to see the same rows the super admin created.
        modelBuilder.Entity<SubscriptionPlan>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedNever();
            e.Property(p => p.Name).HasMaxLength(100).IsRequired();
            e.Property(p => p.Description).HasMaxLength(500);
            e.HasIndex(p => p.Name).IsUnique();
            e.ToTable("SubscriptionPlans");
        });

        modelBuilder.Entity<Subscription>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).ValueGeneratedNever();
            e.Property(s => s.PlanName).HasMaxLength(100).IsRequired();

            // The question asked on every page load is "what is this school's
            // subscription", so that is the index.
            e.HasIndex(s => new { s.SchoolId, s.Status });

            // The daily job scans by end date.
            e.HasIndex(s => s.EndsOn);

            e.Ignore(s => s.GraceEndsOn);

            e.ToTable("Subscriptions");
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedNever();
            e.Property(p => p.RazorpayOrderId).HasMaxLength(64).IsRequired();
            e.Property(p => p.RazorpayPaymentId).HasMaxLength(64);

            // Unique, because the webhook looks a payment up by it — and because
            // a duplicate order id would mean two rows disagreeing about what
            // one payment bought.
            e.HasIndex(p => p.RazorpayOrderId).IsUnique();

            e.HasIndex(p => new { p.SchoolId, p.Status });
            e.ToTable("Payments");
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
            {
                typeof(SchoolDbContext)
                    .GetMethod(
                        nameof(SetTenantFilter),
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    )!
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(this, [modelBuilder]);
            }
        }
    }

    private void SetTenantFilter<T>(ModelBuilder modelBuilder)
        where T : class, ITenantOwned =>
        modelBuilder
            .Entity<T>()
            .HasQueryFilter(e =>
                tenantProvider.TenantId == null || e.TenantId == tenantProvider.TenantId
            );
}
