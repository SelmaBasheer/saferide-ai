using Microsoft.EntityFrameworkCore;
using SafeRide.Ai.Domain.Entities;
using SafeRide.Ai.Domain.Enums;

namespace SafeRide.Ai.Infrastructure.Persistence;

public sealed class AiDbContext(DbContextOptions<AiDbContext> options) : DbContext(options)
{
    public DbSet<Anomaly> Anomalies => Set<Anomaly>();

    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    public DbSet<AnomalyAudit> AnomalyAudits => Set<AnomalyAudit>();

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

            // SQL Server forbids the OUTPUT clause on a table carrying a trigger,
            // and EF uses OUTPUT to read back what it just wrote. Declaring the
            // trigger here makes EF fall back to a separate SELECT instead.
            // Without this line every anomaly update fails with error 334.
            e.ToTable(t => t.HasTrigger("TR_Anomalies_StatusAudit"));
        });

        modelBuilder.Entity<ProcessedEvent>(e =>
        {
            // The event id *is* the key. Two deliveries of the same message can
            // never both insert a row — the database rejects the second one, so
            // deduplication doesn't depend on our code winning a race.
            e.HasKey(p => p.EventId);
            e.Property(p => p.EventType).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<AnomalyAudit>(e =>
        {
            e.HasKey(a => a.Id);

            // Nothing in the application ever writes here, so there is no
            // navigation property and no foreign key — the trigger is the only
            // author, and an audit row must survive even if its anomaly is gone.
            e.HasIndex(a => new { a.AnomalyId, a.ChangedAtUtc });
        });

        // Keyless and view-less: the rows come from a stored procedure, so there
        // is no table for EF to create and no identity for it to track.
        modelBuilder.Entity<RouteAnomalyRow>(e =>
        {
            e.HasNoKey();
            e.ToView(null);
        });
    }
}
