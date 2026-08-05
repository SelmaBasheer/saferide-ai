namespace SafeRide.Schools.Domain.Common;

// Every tenant-owned entity carries its tenant key directly —
// global query filters cannot walk joins, so each row must know its owner.
public interface ITenantOwned
{
    Guid TenantId { get; }
}
