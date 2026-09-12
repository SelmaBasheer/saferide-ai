using Microsoft.EntityFrameworkCore;
using SafeRide.Ai.Domain.Entities;
using SafeRide.Ai.Domain.Enums;

namespace SafeRide.Ai.Infrastructure.Persistence;

public sealed class AiDbContext(DbContextOptions<AiDbContext> options) : DbContext(options)
{
    public DbSet<Anomaly> Anomalies => Set<Anomaly>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Anomaly>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.RouteCode).IsRequired().HasMaxLength(32);
            e.Property(a => a.RouteName).IsRequired().HasMaxLength(200);
            e.Property(a => a.ContextJson).IsRequired();
            e.Property(a => a.Classification).HasMaxLength(64);
            e.Property(a => a.DraftMessage).HasMaxLength(1000);

            // The alerts page lists a school's anomalies newest first.
            e.HasIndex(a => new { a.SchoolId, a.DetectedAtUtc });

            // Used to check whether this trip already has an unresolved anomaly
            // of this type before creating another.
            e.HasIndex(a => new
            {
                a.TripId,
                a.Type,
                a.Status,
            });
        });
    }
}
