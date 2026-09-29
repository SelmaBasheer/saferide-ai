using Dapper;
using SafeRide.Analytics.Abstractions;
using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Data.Repositories;

public sealed class TripFactRepository(IDbConnectionFactory factory) : ITripFactRepository
{
    public async Task InsertAsync(
        TripFactRow trip,
        IReadOnlyList<AttendanceFactRow> roster,
        CancellationToken ct
    )
    {
        using var conn = factory.Create();
        conn.Open();

        using var transaction = conn.BeginTransaction();

        try
        {
            await conn.ExecuteAsync(
                new CommandDefinition(
                    SqlScripts.Get("InsertTripFact"),
                    trip,
                    transaction,
                    cancellationToken: ct
                )
            );

            if (roster.Count > 0)
            {
                // Dapper runs the same statement once per element when handed a
                // list. One round trip per student is fine at this size, and it
                // keeps the SQL readable rather than building a table-valued
                // parameter for twenty rows.
                await conn.ExecuteAsync(
                    new CommandDefinition(
                        SqlScripts.Get("InsertAttendanceFact"),
                        roster,
                        transaction,
                        cancellationToken: ct
                    )
                );
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
