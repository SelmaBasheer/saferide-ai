using System.Data;
using Microsoft.Data.SqlClient;

namespace SafeRide.Analytics.Data;

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration config)
    {
        _connectionString =
            config.GetConnectionString("Analytics")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Analytics is not configured."
            );
    }

    // A new connection per call. SqlClient pools underneath, so this is cheap,
    // and it keeps each query's lifetime explicit rather than sharing one
    // connection across concurrent consumers.
    public IDbConnection Create() => new SqlConnection(_connectionString);
}
