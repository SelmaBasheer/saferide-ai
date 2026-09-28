using Dapper;
using SafeRide.Analytics.Abstractions;
using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Data.Repositories;

public sealed class BusDimensionRepository(IDbConnectionFactory factory) : IBusDimensionRepository
{
    public async Task UpsertAsync(BusDimensionRow row, CancellationToken ct)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync(
            new CommandDefinition(SqlScripts.Get("UpsertBusDimension"), row, cancellationToken: ct)
        );
    }
}
