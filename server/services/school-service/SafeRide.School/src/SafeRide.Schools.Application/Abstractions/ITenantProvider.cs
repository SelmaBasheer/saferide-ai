namespace SafeRide.Schools.Application.Abstractions;

public interface ITenantProvider
{
    // Current tenant (SchoolId) — null for SuperAdmin, anonymous requests,
    // and non-HTTP contexts (message consumers, background jobs).
    Guid? TenantId { get; }
}
