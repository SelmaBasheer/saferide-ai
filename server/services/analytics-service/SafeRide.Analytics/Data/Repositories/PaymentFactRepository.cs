using Dapper;
using SafeRide.Analytics.Abstractions;
using SafeRide.Analytics.Models;

namespace SafeRide.Analytics.Data.Repositories;

public sealed class PaymentFactRepository(IDbConnectionFactory factory) : IPaymentFactRepository
{
    public async Task InsertAsync(PaymentFactRow row, CancellationToken ct)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync(
            new CommandDefinition(SqlScripts.Get("InsertPaymentFact"), row, cancellationToken: ct)
        );
    }
}
