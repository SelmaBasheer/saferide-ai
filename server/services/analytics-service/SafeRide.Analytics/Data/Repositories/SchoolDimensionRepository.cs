using Dapper;
using SafeRide.Analytics.Abstractions;
using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Data.Repositories;

public sealed class SchoolDimensionRepository(IDbConnectionFactory factory)
    : ISchoolDimensionRepository
{
    public async Task UpsertStatusAsync(SchoolStatusRow row, CancellationToken ct)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync(
            new CommandDefinition(SqlScripts.Get("UpsertSchoolStatus"), row, cancellationToken: ct)
        );
    }

    public async Task UpdateEntitlementAsync(SchoolEntitlementRow row, CancellationToken ct)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync(
            new CommandDefinition(
                SqlScripts.Get("UpdateSchoolEntitlement"),
                row,
                cancellationToken: ct
            )
        );
    }
}
