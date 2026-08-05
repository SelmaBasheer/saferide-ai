using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Domain.Common;

namespace SafeRide.Schools.Infrastructure.Persistence;

public sealed class TenantStampInterceptor(ITenantProvider tenantProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
            return;

        foreach (var entry in context.ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State != EntityState.Added)
                continue;

            if (entry.Entity.TenantId == Guid.Empty)
            {
                // Backstop: entity arrived unowned — stamp it, or refuse.
                var tenant =
                    tenantProvider.TenantId
                    ?? throw new InvalidOperationException(
                        $"Inserting {entry.Metadata.ClrType.Name} requires a tenant context."
                    );
                entry.Property(nameof(ITenantOwned.TenantId)).CurrentValue = tenant;
            }
            else if (tenantProvider.TenantId is Guid current && current != entry.Entity.TenantId)
            {
                // Domain set one tenant, the request belongs to another — never allow.
                throw new InvalidOperationException(
                    $"Cross-tenant insert blocked for {entry.Metadata.ClrType.Name}."
                );
            }
        }
    }
}
